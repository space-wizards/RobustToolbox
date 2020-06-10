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
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NetSerializer;
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

            private static readonly ISawmill _sawmill  = Logger.GetSawmill("szr");

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
                    _sawmill.Info($"Locked in at {_MappedStrings.Count} mapped strings.");
                }

                IncompleteHandshakes.Add(channel);

                var message = net.CreateNetMessage<MsgServerHandshake>();
                message.Hash = MappedStringsHash;
                net.ServerSendMessage(message, channel);

                while (IncompleteHandshakes.Contains(channel))
                {
                    await Task.Delay(1);
                }

                _sawmill.Info($"Completed handshake with {channel.RemoteEndPoint.Address}.");
            }

            public static void NetworkInitialize(INetManager net)
            {
                net.RegisterNetMessage<MsgServerHandshake>(
                    $"{nameof(RobustSerializer)}.{nameof(MappedStringSerializer)}.{nameof(MsgServerHandshake)}",
                    (msg) =>
                    {
                        if (net.IsServer)
                        {
                            _sawmill.Error("Received server handshake on server.");
                            return;
                        }

                        ServerHash = msg.Hash;
                        LockMappedStrings = false;

                        var cached = ReadStringCache(msg.Hash!, out var hashStr);

                        _sawmill.Info($"Received server handshake with hash {hashStr}.");

                        if (cached)
                        {
                            _sawmill.Info($"We had a cached string map that matches {hashStr}.");
                            LockMappedStrings = true;
                            _sawmill.Info($"Locked in at {_MappedStrings.Count} mapped strings.");
                            // ok we're good now
                            var channel = msg.MsgChannel;
                            OnClientCompleteHandshake(net, channel);
                        }
                        else
                        {
                            var handshake = net.CreateNetMessage<MsgClientHandshake>();
                            _sawmill.Info("Asking server to send mapped strings.");
                            handshake.NeedsStrings = true;
                            msg.MsgChannel.SendMessage(handshake);
                        }
                    });

                net.RegisterNetMessage<MsgClientHandshake>(
                    $"{nameof(RobustSerializer)}.{nameof(MappedStringSerializer)}.{nameof(MsgClientHandshake)}",
                    (msg) =>
                    {
                        if (net.IsClient)
                        {
                            _sawmill.Error("Received client handshake on client.");
                            return;
                        }
                        _sawmill.Info($"Received handshake from {msg.MsgChannel.RemoteEndPoint.Address}.");

                        if (!msg.NeedsStrings)
                        {
                            _sawmill.Info($"Completing handshake with {msg.MsgChannel.RemoteEndPoint.Address}.");
                            IncompleteHandshakes.Remove(msg.MsgChannel);
                            return;
                        }

                        var strings = msg.MsgChannel.NetPeer.CreateNetMessage<MsgStrings>();
                        using (var ms = new MemoryStream())
                        {
                            WriteStringPackage(ms);
                            ms.Position = 0;
                            strings.Package = ms.ToArray();
                            _sawmill.Info($"Sending {ms.Length} bytes sized mapped strings package to {msg.MsgChannel.RemoteEndPoint.Address}.");
                        }

                        msg.MsgChannel.SendMessage(strings);
                    });

                net.RegisterNetMessage<MsgStrings>(
                    $"{nameof(RobustSerializer)}.{nameof(MappedStringSerializer)}.{nameof(MsgStrings)}",
                    (msg) =>
                    {
                        if (net.IsServer)
                        {
                            _sawmill.Error("Received strings from client.");
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

                        _sawmill.Info($"Locked in at {_MappedStrings.Count} mapped strings.");

                        WriteStringCache();

                        // ok we're good now
                        var channel = msg.MsgChannel;
                        OnClientCompleteHandshake(net, channel);
                    });
            }

            private static void OnClientCompleteHandshake(INetManager net, INetChannel channel)
            {
                _sawmill.Info("Letting server know we're good to go.");
                var handshake = net.CreateNetMessage<MsgClientHandshake>();
                handshake.NeedsStrings = false;
                channel.SendMessage(handshake);

                if (ClientHandshakeComplete == null)
                {
                    _sawmill.Warning("There's no handler attached to ClientHandshakeComplete.");
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

                _sawmill.Info($"Wrote string cache {hashStr}.");
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

                _sawmill.Info($"Wrote {MappedStrings.Count} strings to package in {sw.ElapsedMilliseconds}ms.");
            }

            private static bool ReadStringCache(byte[] hash, out string hashStr)
            {
                if (LockMappedStrings)
                {
                    throw new InvalidOperationException("Mapped strings are locked.");
                }

                ClearStrings();

                hashStr = Convert.ToBase64String(hash);
                hashStr = ConvertToBase64Url(hashStr);

                var fileName = PathHelpers.ExecutableRelativeFile("strings-" + hashStr);
                if (!File.Exists(fileName))
                {
                    _sawmill.Info($"No string cache for {hashStr}.");
                    return false;
                }

                using var file = File.OpenRead(fileName);
                var added = LoadStrings(file);

                _stringMapHash = hash;
                _sawmill.Info($"Read {added} strings from cache {hashStr}.");
                return true;
            }

            private static int LoadStrings(Stream stream)
            {
                if (LockMappedStrings)
                {
                    throw new InvalidOperationException("Mapped strings are locked, will not add.");
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

                _sawmill.Info($"Read package of {c} strings in {sw.ElapsedMilliseconds}ms.");
            }

            private static string ConvertToBase64Url(byte[]? data)
                => data == null ? "" : ConvertToBase64Url(Convert.ToBase64String(data));

            private static string ConvertToBase64Url(string b64Str)
            {
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

            public static bool AddString(string str)
            {
                if (LockMappedStrings)
                {
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

                str = str.Trim();

                str = str.Replace(Environment.NewLine, "\n");

                if (str.Contains('/'))
                {
                    var parts = str.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    for (var i = 0; i < parts.Length; ++i)
                    {
                        for (var l = 1; l <= parts.Length - i; ++l)
                        {
                            var subStr = string.Join('/', parts.Skip(i).Take(l));
                            if (!_StringMapping.ContainsKey(subStr))
                            {
                                _StringMapping[subStr] = _MappedStrings.Count;
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
                                    if (_StringMapping.ContainsKey(subSubStr))
                                    {
                                        continue;
                                    }

                                    _StringMapping[subSubStr] = _MappedStrings.Count;
                                    _MappedStrings.Add(subSubStr);
                                }
                            }
                        }
                    }
                }

                _StringMapping[str] = _MappedStrings.Count;
                _MappedStrings.Add(str);
                _stringMapHash = null;
                _mappedStringsPackage = null;
                return true;
            }

            public static unsafe void AddStrings(Assembly asm)
            {
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
                _sawmill.Info($"Mapping {added} strings from {asm.GetName().Name} took {sw.ElapsedMilliseconds}ms.");
            }

            public static void AddStrings(YamlStream yaml)
            {
                var started = MappedStrings.Count;
                var sw = Stopwatch.StartNew();
                foreach (var doc in yaml)
                {
                    foreach (var node in doc.AllNodes)
                    {
                        if (!(node is YamlScalarNode scalar))
                        {
                            continue;
                        }

                        var s = scalar.Value;
                        if (string.IsNullOrEmpty(s))
                        {
                            continue;
                        }

                        AddString(s);
                    }
                }

                var added = MappedStrings.Count - started;
                _sawmill.Info($"Mapping {added} strings from YAML took {sw.ElapsedMilliseconds}ms.");
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

            public static void AddStrings(IEnumerable<string> strings)
            {
                var started = MappedStrings.Count;
                foreach (var str in strings)
                {
                    AddString(str);
                }

                var added = MappedStrings.Count - started;
                _sawmill.Info($"Mapping {added} strings.");
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

                _sawmill.Info($"Hashing {MappedStrings.Count} strings took {sw.ElapsedMilliseconds}ms.");
                _sawmill.Info($"Size: {MappedStringsPackage.Length} bytes, Hash: {ConvertToBase64Url(hash)}");
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

            public static void WriteMappedString(Stream stream, string? value)
            {
                if (value == null)
                {
                    WriteCompressedUnsignedInt(stream, 0);
                    return;
                }

                if (_StringMapping.TryGetValue(value, out var mapping))
                {
                    WriteCompressedUnsignedInt(stream, (uint) mapping + 2);
                    return;
                }

                // indicate not mapped
                WriteCompressedUnsignedInt(stream, 1);
                var buf = Encoding.UTF8.GetBytes(value);
                WriteCompressedUnsignedInt(stream, (uint) buf.Length + 1);
                stream.Write(buf);
            }

            public static void ReadMappedString(Stream stream, out string? value)
            {
                if (!LockMappedStrings)
                {
                    throw new InvalidOperationException("Not performing unlocked string mapping.");
                }

                var mapIndex = ReadCompressedUnsignedInt(stream);
                if (mapIndex == 0)
                {
                    value = null;
                    return;
                }

                if (mapIndex == 1)
                {
                    // not mapped
                    var length = ReadCompressedUnsignedInt(stream);
                    var buf = new byte[length - 1];
                    stream.Read(buf);
                    value = Encoding.UTF8.GetString(buf);
                    return;
                }

                value = _MappedStrings[(int) mapIndex - 2];
            }

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

            public static uint ReadCompressedUnsignedInt(Stream stream)
            {
                var value = 0u;
                var shift = 0;
                while (stream.CanRead)
                {
                    var current = stream.ReadByte();
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

            public static event Action? ClientHandshakeComplete;

        }

    }

}
