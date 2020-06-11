using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NetSerializer;
using Newtonsoft.Json.Linq;
using Robust.Shared.ContentPack;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization
{

    public partial class RobustSerializer
    {

        public partial class MappedStringSerializer : IStaticTypeSerializer
        {

            private static readonly INetManager NetManager = IoCManager.Resolve<INetManager>();

            private static readonly ISawmill LogSzr = Logger.GetSawmill("szr");

            private static readonly HashSet<INetChannel> IncompleteHandshakes = new HashSet<INetChannel>();

            public static async Task Handshake(INetChannel channel)
            {
                var net = channel.NetPeer;

                if (net.IsClient)
                {
                    return;
                }

                if (!LockMappedStrings)
                {
                    LockMappedStrings = true;
                    LogSzr.Info($"Locked in at {_MappedStrings.Count} mapped strings.");
                }

                IncompleteHandshakes.Add(channel);

                var message = net.CreateNetMessage<MsgServerHandshake>();
                message.Hash = MappedStringsHash;
                net.ServerSendMessage(message, channel);

                while (IncompleteHandshakes.Contains(channel))
                {
                    await Task.Delay(1);
                }

                LogSzr.Info($"Completed handshake with {channel.RemoteEndPoint.Address}.");
            }

            public static void NetworkInitialize(INetManager net)
            {
                net.RegisterNetMessage<MsgServerHandshake>(
                    $"{nameof(RobustSerializer)}.{nameof(MappedStringSerializer)}.{nameof(MsgServerHandshake)}",
                    (msg) =>
                    {
                        if (net.IsServer)
                        {
                            LogSzr.Error("Received server handshake on server.");
                            return;
                        }

                        ServerHash = msg.Hash;
                        LockMappedStrings = false;

                        if (LockMappedStrings)
                        {
                            throw new InvalidOperationException("Mapped strings are locked.");
                        }

                        ClearStrings();

                        var hashStr = ConvertToBase64Url(Convert.ToBase64String(msg.Hash!));

                        LogSzr.Info($"Received server handshake with hash {hashStr}.");

                        var fileName = PathHelpers.ExecutableRelativeFile("strings-" + hashStr);
                        if (!File.Exists(fileName))
                        {
                            LogSzr.Info($"No string cache for {hashStr}.");
                            var handshake = net.CreateNetMessage<MsgClientHandshake>();
                            LogSzr.Info("Asking server to send mapped strings.");
                            handshake.NeedsStrings = true;
                            msg.MsgChannel.SendMessage(handshake);
                        }
                        else
                        {
                            LogSzr.Info($"We had a cached string map that matches {hashStr}.");
                            using var file = File.OpenRead(fileName);
                            var added = LoadStrings(file);

                            _stringMapHash = msg.Hash!;
                            LogSzr.Info($"Read {added} strings from cache {hashStr}.");
                            LockMappedStrings = true;
                            LogSzr.Info($"Locked in at {_MappedStrings.Count} mapped strings.");
                            // ok we're good now
                            var channel = msg.MsgChannel;
                            OnClientCompleteHandshake(net, channel);
                        }
                    });

                net.RegisterNetMessage<MsgClientHandshake>(
                    $"{nameof(RobustSerializer)}.{nameof(MappedStringSerializer)}.{nameof(MsgClientHandshake)}",
                    (msg) =>
                    {
                        if (net.IsClient)
                        {
                            LogSzr.Error("Received client handshake on client.");
                            return;
                        }

                        LogSzr.Info($"Received handshake from {msg.MsgChannel.RemoteEndPoint.Address}.");

                        if (!msg.NeedsStrings)
                        {
                            LogSzr.Info($"Completing handshake with {msg.MsgChannel.RemoteEndPoint.Address}.");
                            IncompleteHandshakes.Remove(msg.MsgChannel);
                            return;
                        }

                        var strings = msg.MsgChannel.NetPeer.CreateNetMessage<MsgStrings>();
                        using (var ms = new MemoryStream())
                        {
                            WriteStringPackage(ms);
                            ms.Position = 0;
                            strings.Package = ms.ToArray();
                            LogSzr.Info($"Sending {ms.Length} bytes sized mapped strings package to {msg.MsgChannel.RemoteEndPoint.Address}.");
                        }

                        msg.MsgChannel.SendMessage(strings);
                    });

                net.RegisterNetMessage<MsgStrings>(
                    $"{nameof(RobustSerializer)}.{nameof(MappedStringSerializer)}.{nameof(MsgStrings)}",
                    (msg) =>
                    {
                        if (net.IsServer)
                        {
                            LogSzr.Error("Received strings from client.");
                            return;
                        }

                        LockMappedStrings = false;
                        ClearStrings();
                        DebugTools.Assert(msg.Package != null, "msg.Package != null");
                        LoadStrings(new MemoryStream(msg.Package!, false));
                        var checkHash = CalculateHash(msg.Package!);
                        if (!checkHash.SequenceEqual(ServerHash))
                        {
                            throw new InvalidOperationException("Unable to verify strings package by hash." +
                                $"\n{ConvertToBase64Url(checkHash)} vs. {ConvertToBase64Url(ServerHash)}");
                        }

                        _stringMapHash = ServerHash;
                        LockMappedStrings = true;

                        LogSzr.Info($"Locked in at {_MappedStrings.Count} mapped strings.");

                        WriteStringCache();

                        // ok we're good now
                        var channel = msg.MsgChannel;
                        OnClientCompleteHandshake(net, channel);
                    });
            }

            private static void OnClientCompleteHandshake(INetManager net, INetChannel channel)
            {
                LogSzr.Info("Letting server know we're good to go.");
                var handshake = net.CreateNetMessage<MsgClientHandshake>();
                handshake.NeedsStrings = false;
                channel.SendMessage(handshake);

                if (ClientHandshakeComplete == null)
                {
                    LogSzr.Warning("There's no handler attached to ClientHandshakeComplete.");
                }

                ClientHandshakeComplete?.Invoke();
            }

            private static void WriteStringCache()
            {
                var hashStr = Convert.ToBase64String(MappedStringsHash);
                hashStr = ConvertToBase64Url(hashStr);

                var fileName = PathHelpers.ExecutableRelativeFile("strings-" + hashStr);
                using var file = File.OpenWrite(fileName);
                WriteStringPackage(file);

                LogSzr.Info($"Wrote string cache {hashStr}.");
            }

            private static byte[]? _mappedStringsPackage;

            private static byte[] MappedStringsPackage => LockMappedStrings
                ? _mappedStringsPackage ??= WriteStringPackage()
                : throw new InvalidOperationException("Mapped strings must be locked.");

            private static byte[] WriteStringPackage()
            {
                using var ms = new MemoryStream();
                WriteStringPackage(ms);
                return ms.ToArray();
            }

            /// <summary>
            ///     Strings longer than this will throw an exception and a better strategy will need to be employed to deal with large strings.
            /// <summary>
            public static int StringPackageMaximumBufferSize = 65536;

            public static void WriteStringPackage(Stream stream)
            {
                var buf = new byte[StringPackageMaximumBufferSize];
                var sw = Stopwatch.StartNew();
                var enc = Encoding.UTF8.GetEncoder();

                using (var zs = new DeflateStream(stream, CompressionLevel.Optimal, true))
                {
                    var bytesWritten = WriteCompressedUnsignedInt(zs, (uint) MappedStrings.Count);
                    foreach (var str in MappedStrings)
                    {
                        if (str.Length >= StringPackageMaximumBufferSize)
                        {
                            throw new NotImplementedException("Overly long string in strings package.");
                        }

                        var l = enc.GetBytes(str, buf, true);

                        if (l >= StringPackageMaximumBufferSize)
                        {
                            throw new NotImplementedException("Overly long string in strings package.");
                        }

                        bytesWritten += WriteCompressedUnsignedInt(zs, (uint) l);

                        zs.Write(buf, 0, l);

                        bytesWritten += l;

                        enc.Reset();
                    }

                    zs.Write(BitConverter.GetBytes(bytesWritten));
                    zs.Flush();
                }

                LogSzr.Info($"Wrote {MappedStrings.Count} strings to package in {sw.ElapsedMilliseconds}ms.");
            }

            private static int LoadStrings(Stream stream)
            {
                if (LockMappedStrings)
                {
                    throw new InvalidOperationException("Mapped strings are locked, will not load.");
                }

                var started = MappedStrings.Count;
                foreach (var str in ReadStringPackage(stream))
                {
                    _StringMapping[str] = _MappedStrings.Count;
                    _MappedStrings.Add(str);
                }

                if (stream.CanSeek && stream.CanRead)
                {
                    if (stream.Position != stream.Length)
                    {
                        throw new Exception("Did not read all bytes in package!");
                    }
                }

                var added = MappedStrings.Count - started;
                return added;
            }

            private static IEnumerable<string> ReadStringPackage(Stream stream)
            {
                var buf = ArrayPool<byte>.Shared.Rent(65536);
                var sw = Stopwatch.StartNew();
                using var zs = new DeflateStream(stream, CompressionMode.Decompress);

                var c = ReadCompressedUnsignedInt(zs, out var x);
                var bytesRead = x;
                for (var i = 0; i < c; ++i)
                {
                    var l = (int) ReadCompressedUnsignedInt(zs, out x);
                    bytesRead += x;
                    var y = zs.Read(buf, 0, l);
                    if (y != l)
                    {
                        throw new InvalidDataException($"Could not read the full length of string #{i}.");
                    }

                    bytesRead += y;
                    var str = Encoding.UTF8.GetString(buf, 0, l);
                    yield return str;
                }

                zs.Read(buf, 0, 4);
                var checkBytesRead = BitConverter.ToInt32(buf, 0);
                if (checkBytesRead != bytesRead)
                {
                    throw new InvalidDataException("Could not verify package was read correctly.");
                }

                LogSzr.Info($"Read package of {c} strings in {sw.ElapsedMilliseconds}ms.");
            }

            private static string ConvertToBase64Url(byte[]? data)
                => data == null ? "" : ConvertToBase64Url(Convert.ToBase64String(data));

            private static string ConvertToBase64Url(string b64Str)
            {
                if (b64Str is null)
                {
                    throw new ArgumentNullException(nameof(b64Str));
                }

                var cut = b64Str[^1] == '=' ? b64Str[^2] == '=' ? 2 : 1 : 0;
                b64Str = new StringBuilder(b64Str).Replace('+', '-').Replace('/', '_').ToString(0, b64Str.Length - cut);
                return b64Str;
            }

            public static byte[] ConvertFromBase64Url(string s)
            {
                var l = s.Length % 3;
                var sb = new StringBuilder(s);
                sb.Replace('-', '+').Replace('_', '/');
                for (var i = 0; i < l; ++i)
                {
                    sb.Append('=');
                }

                s = sb.ToString();
                return Convert.FromBase64String(s);
            }

            public static byte[]? ServerHash;

            private static readonly IList<string> _MappedStrings = new List<string>();

            private static readonly IDictionary<string, int> _StringMapping = new Dictionary<string, int>();

            public static IReadOnlyList<String> MappedStrings => new ReadOnlyCollection<string>(_MappedStrings);

            public static bool LockMappedStrings { get; set; }

            private static readonly Regex RxSymbolSplitter
                = new Regex(
                    @"(?<=[^\s\W])(?=[A-Z])|(?<=[^0-9\s\W])(?=[0-9])|(?<=[A-Za-z0-9])(?=_)|(?=[.\\\/,#$?!@|&*()^`""'`~[\]{}:;\-])|(?<=[.\\\/,#$?!@|&*()^`""'`~[\]{}:;\-])",
                    RegexOptions.CultureInvariant | RegexOptions.Compiled
                );

            public static bool AddString(string str)
            {
                if (LockMappedStrings)
                {
                    if (NetManager.IsClient)
                    {
                        //LogSzr.Info("On client and mapped strings are locked, will not add.");
                        return false;
                    }
                    throw new InvalidOperationException("Mapped strings are locked, will not add.");
                }

                if (string.IsNullOrEmpty(str))
                {
                    return false;
                }

                if (!str.IsNormalized())
                {
                    throw new InvalidOperationException("Only normalized strings may be added.");
                }

                if (_StringMapping.ContainsKey(str))
                {
                    return false;
                }

                if (str.Length >= MaxMappedStringSize) return false;

                if (str.Length <= 3) return false;

                str = str.Trim();

                if (str.Length <= 3) return false;

                str = str.Replace(Environment.NewLine, "\n");

                if (str.Length <= 3) return false;

                var symTrimmedStr = str.Trim(TrimmableSymbolChars);
                if (symTrimmedStr != str)
                {
                    AddString(symTrimmedStr);
                }

                if (str.Contains('/'))
                {
                    var parts = str.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    for (var i = 0; i < parts.Length; ++i)
                    {
                        for (var l = 1; l <= parts.Length - i; ++l)
                        {
                            var subStr = string.Join('/', parts.Skip(i).Take(l));
                            if (_StringMapping.TryAdd(subStr, _MappedStrings.Count))
                            {
                                _MappedStrings.Add(subStr);
                            }

                            if (!subStr.Contains('.'))
                            {
                                continue;
                            }

                            var subParts = subStr.Split('.', StringSplitOptions.RemoveEmptyEntries);
                            for (var si = 0; si < subParts.Length; ++si)
                            {
                                for (var sl = 1; sl <= subParts.Length - si; ++sl)
                                {
                                    var subSubStr = string.Join('.', subParts.Skip(si).Take(sl));
                                    // ReSharper disable once InvertIf
                                    if (_StringMapping.TryAdd(subSubStr, _MappedStrings.Count))
                                    {
                                        _MappedStrings.Add(subSubStr);
                                    }
                                }
                            }
                        }
                    }
                }
                else if (str.Contains("_"))
                {
                    foreach (var substr in str.Split("_"))
                    {
                        AddString(substr);
                    }
                }
                else if (str.Contains(" "))
                {
                    foreach (var substr in str.Split(" "))
                    {
                        if (substr == str) continue;

                        AddString(substr);
                    }
                }
                else
                {
                    var parts = RxSymbolSplitter.Split(str);
                    foreach (var substr in parts)
                    {
                        if (substr == str) continue;

                        AddString(substr);
                    }

                    for (var si = 0; si < parts.Length; ++si)
                    {
                        for (var sl = 1; sl <= parts.Length - si; ++sl)
                        {
                            var subSubStr = string.Concat(parts.Skip(si).Take(sl));
                            if (_StringMapping.TryAdd(subSubStr, _MappedStrings.Count))
                            {
                                _MappedStrings.Add(subSubStr);
                            }
                        }
                    }
                }

                if (_StringMapping.TryAdd(str, _MappedStrings.Count))
                {
                    _MappedStrings.Add(str);
                }

                _stringMapHash = null;
                _mappedStringsPackage = null;
                return true;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public static unsafe void AddStrings(Assembly asm)
            {
                if (LockMappedStrings)
                {
                    if (NetManager.IsClient)
                    {
                        //LogSzr.Info("On client and mapped strings are locked, will not add.");
                        return;
                    }
                    throw new InvalidOperationException("Mapped strings are locked, will not add .");
                }

                var started = MappedStrings.Count;
                var sw = Stopwatch.StartNew();
                if (asm.TryGetRawMetadata(out var blob, out var len))
                {
                    var reader = new MetadataReader(blob, len);
                    var usrStrHandle = default(UserStringHandle);
                    do
                    {
                        var userStr = reader.GetUserString(usrStrHandle);
                        if (userStr != "")
                        {
                            AddString(string.Intern(userStr.Normalize()));
                        }

                        usrStrHandle = reader.GetNextHandle(usrStrHandle);
                    } while (usrStrHandle != default);

                    var strHandle = default(StringHandle);
                    do
                    {
                        var str = reader.GetString(strHandle);
                        if (str != "")
                        {
                            AddString(string.Intern(str.Normalize()));
                        }

                        strHandle = reader.GetNextHandle(strHandle);
                    } while (strHandle != default);
                }

                var added = MappedStrings.Count - started;
                LogSzr.Info($"Mapping {added} strings from {asm.GetName().Name} took {sw.ElapsedMilliseconds}ms.");
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public static void AddStrings(YamlStream yaml, string name)
            {
                if (LockMappedStrings)
                {
                    if (NetManager.IsClient)
                    {
                        //LogSzr.Info("On client and mapped strings are locked, will not add.");
                        return;
                    }
                    throw new InvalidOperationException("Mapped strings are locked, will not add.");
                }

                var started = MappedStrings.Count;
                var sw = Stopwatch.StartNew();
                foreach (var doc in yaml)
                {
                    foreach (var node in doc.AllNodes)
                    {
                        var a = node.Anchor;
                        if (!string.IsNullOrEmpty(a))
                        {
                            AddString(a);
                        }

                        var t = node.Tag;
                        if (!string.IsNullOrEmpty(t))
                        {
                            AddString(t);
                        }

                        switch (node)
                        {
                            case YamlScalarNode scalar:
                            {
                                var v = scalar.Value;
                                if (string.IsNullOrEmpty(v))
                                {
                                    continue;
                                }

                                AddString(v);
                                break;
                            }
                        }
                    }
                }

                var added = MappedStrings.Count - started;
                LogSzr.Info($"Mapping {added} strings from {name} took {sw.ElapsedMilliseconds}ms.");
            }


            public static void AddStrings(JObject obj, string name)
            {
                if (LockMappedStrings)
                {
                    if (NetManager.IsClient)
                    {
                        //LogSzr.Info("On client and mapped strings are locked, will not add.");
                        return;
                    }
                    throw new InvalidOperationException("Mapped strings are locked, will not add.");
                }

                var started = MappedStrings.Count;
                var sw = Stopwatch.StartNew();
                foreach (var node in obj.DescendantsAndSelf())
                {
                    switch (node)
                    {
                        case JValue value:
                        {
                            if (value.Type != JTokenType.String)
                            {
                                continue;
                            }

                            var v = value.Value?.ToString();
                            if (string.IsNullOrEmpty(v))
                            {
                                continue;
                            }

                            AddString(v);
                            break;
                        }
                        case JProperty prop:
                        {
                            var propName = prop.Name;
                            if (string.IsNullOrEmpty(propName))
                            {
                                continue;
                            }

                            AddString(propName);
                            break;
                        }
                    }
                }

                var added = MappedStrings.Count - started;
                LogSzr.Info($"Mapping {added} strings from {name} took {sw.ElapsedMilliseconds}ms.");
            }

            public static void ClearStrings()
            {
                if (LockMappedStrings)
                {
                    throw new InvalidOperationException("Mapped strings are locked, will not clear.");
                }

                _MappedStrings.Clear();
                _StringMapping.Clear();
                _stringMapHash = null;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public static void AddStrings(IEnumerable<string> strings)
            {
                var started = MappedStrings.Count;
                foreach (var str in strings)
                {
                    AddString(str);
                }

                var added = MappedStrings.Count - started;
                LogSzr.Info($"Mapping {added} strings.");
            }

            private static byte[]? _stringMapHash;

            public static byte[] MappedStringsHash => _stringMapHash ??= CalculateMappedStringsHash();

            private static byte[] CalculateMappedStringsHash()
            {
                if (!LockMappedStrings)
                {
                    throw new InvalidOperationException("String table should be locked before attempting to retrieve hash.");
                }

                var sw = Stopwatch.StartNew();

                var hash = CalculateHash(MappedStringsPackage);

                LogSzr.Info($"Hashing {MappedStrings.Count} strings took {sw.ElapsedMilliseconds}ms.");
                LogSzr.Info($"Size: {MappedStringsPackage.Length} bytes, Hash: {ConvertToBase64Url(hash)}");
                return hash;
            }

            private static byte[] CalculateHash(byte[] data)
            {
                if (data is null)
                {
                    throw new ArgumentNullException(nameof(data));
                }

                using var hasher = SHA512.Create();
                var hash = hasher.ComputeHash(data);
                return hash;
            }

            public bool Handles(Type type) => type == typeof(string);

            public IEnumerable<Type> GetSubtypes(Type type) => Type.EmptyTypes;

            public MethodInfo GetStaticWriter(Type type) => WriteMappedStringMethodInfo;

            public MethodInfo GetStaticReader(Type type) => ReadMappedStringMethodInfo;

            private delegate void WriteStringDelegate(Stream stream, string? value);

            private delegate void ReadStringDelegate(Stream stream, out string? value);

            private static readonly MethodInfo WriteMappedStringMethodInfo = ((WriteStringDelegate) WriteMappedString).Method;

            private static readonly MethodInfo ReadMappedStringMethodInfo = ((ReadStringDelegate) ReadMappedString).Method;

            private static readonly char[] TrimmableSymbolChars =
            {
                '.', '\\', '/', ',', '#', '$', '?', '!', '@', '|', '&', '*', '(', ')', '^', '`', '"', '\'', '`', '~', '[', ']', '{', '}', ':', ';', '-'
            };

            private const int MaxMappedStringSize = 420;

            private const int MappedNull = 0;

            private const int UnmappedString = 1;

            private const int FirstMappedIndexStart = 2;

            public static void WriteMappedString(Stream stream, string? value)
            {
                if (value == null)
                {
                    WriteCompressedUnsignedInt(stream, MappedNull);
                    return;
                }

                if (_StringMapping.TryGetValue(value, out var mapping))
                {
#if DEBUG
                    if (mapping >= _MappedStrings.Count || mapping < 0)
                    {
                        throw new InvalidOperationException("A string mapping outside of the mapped string table was encountered.");
                    }
#endif
                    WriteCompressedUnsignedInt(stream, (uint) mapping + FirstMappedIndexStart);
                    //Logger.DebugS("szr", $"Encoded mapped string: {value}");
                    return;
                }

                // indicate not mapped
                WriteCompressedUnsignedInt(stream, UnmappedString);
                var buf = Encoding.UTF8.GetBytes(value);
                //Logger.DebugS("szr", $"Encoded unmapped string: {value}");
                WriteCompressedUnsignedInt(stream, (uint) buf.Length);
                stream.Write(buf);
            }

            public static void ReadMappedString(Stream stream, out string? value)
            {
                if (!LockMappedStrings)
                {
                    throw new InvalidOperationException("Not performing unlocked string mapping.");
                }


                var mapIndex = ReadCompressedUnsignedInt(stream, out _);
                if (mapIndex == MappedNull)
                {
                    value = null;
                    return;
                }

                if (mapIndex == UnmappedString)
                {
                    // not mapped
                    var length = ReadCompressedUnsignedInt(stream, out _);
                    var buf = new byte[length];
                    stream.Read(buf);
                    value = Encoding.UTF8.GetString(buf);
                    //Logger.DebugS("szr", $"Decoded unmapped string: {value}");
                    return;
                }

                value = _MappedStrings[(int) mapIndex - FirstMappedIndexStart];
                //Logger.DebugS("szr", $"Decoded mapped string: {value}");
            }

#if ROBUST_SERIALIZER_DISABLE_COMPRESSED_UINTS
            public static int WriteCompressedUnsignedInt(Stream stream, uint value)
            {
                WriteUnsignedInt(stream, value);
                return 4;
            }

            public static uint ReadCompressedUnsignedInt(Stream stream, out int byteCount)
            {
                byteCount = 4;
                return ReadUnsignedInt(stream);
            }
#else
            public static int WriteCompressedUnsignedInt(Stream stream, uint value)
            {
                var length = 1;
                while (value >= 0x80)
                {
                    stream.WriteByte((byte) (0x80 | value));
                    value >>= 7;
                    ++length;
                }

                stream.WriteByte((byte) value);
                return length;
            }
            public static uint ReadCompressedUnsignedInt(Stream stream, out int byteCount)
            {
                byteCount = 0;
                var value = 0u;
                var shift = 0;
                while (stream.CanRead)
                {
                    var current = stream.ReadByte();
                    ++byteCount;
                    if (current == -1)
                    {
                        throw new EndOfStreamException();
                    }

                    value |= (0x7Fu & (byte) current) << shift;
                    shift += 7;
                    if ((0x80 & current) == 0)
                    {
                        return value;
                    }
                }

                throw new EndOfStreamException();
            }
#endif

            [UsedImplicitly]
            public static unsafe void WriteUnsignedInt(Stream stream, uint value)
            {
                var bytes = MemoryMarshal.AsBytes(new ReadOnlySpan<uint>(&value, 1));
                stream.Write(bytes);
            }

            [UsedImplicitly]
            public static unsafe uint ReadUnsignedInt(Stream stream)
            {
                uint value;
                var bytes = MemoryMarshal.AsBytes(new Span<uint>(&value, 1));
                stream.Read(bytes);
                return value;
            }

            public static event Action? ClientHandshakeComplete;


        }

    }

}
