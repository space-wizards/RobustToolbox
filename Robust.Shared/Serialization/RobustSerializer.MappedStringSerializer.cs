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

        /// <summary>
        /// Serializer which manages a mapping of pre-loaded strings to constant
        /// values, for message compression. The mapping is shared between the
        /// server and client.
        /// </summary>
        /// <remarks>
        /// Strings are long and expensive to send over the wire, and lots of
        /// strings involved in messages are sent repeatedly between the server
        /// and client - such as filenames, icon states, constant strings, etc.
        ///
        /// To compress these strings, we use a constant string mapping, decided
        /// by the server when it starts up, that associates strings with a
        /// fixed value. The mapping is shared with clients when they connect.
        ///
        /// When sending these strings over the wire, the serializer can then
        /// send the constant value instead - and at the other end, the
        /// serializer can use the same mapping to recover the original string.
        /// </remarks>
        public partial class MappedStringSerializer : IStaticTypeSerializer
        {

            private static INetManager? _net;

            private static readonly ISawmill LogSzr = Logger.GetSawmill("szr");

            private static readonly HashSet<INetChannel> IncompleteHandshakes = new HashSet<INetChannel>();

            /// <summary>
            /// Starts the handshake from the server end of the given channel,
            /// sending a <see cref="MsgServerHandshake"/>.
            /// </summary>
            /// <param name="channel">The network channel to perform the handshake over.</param>
            /// <remarks>
            /// Locks the string mapping if this is the first time the server is
            /// performing the handshake.
            /// </remarks>
            /// <seealso cref="MsgClientHandshake"/>
            /// <seealso cref="MsgStrings"/>
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

            /// <summary>
            /// Performs the setup so that the serializer can perform the string-
            /// exchange protocol.
            /// </summary>
            /// <remarks>
            /// The string-exchange protocol is started by the server when the
            /// client first connects. The server sends the client a hash of the
            /// string mapping; the client checks that hash against any local
            /// caches; and if necessary, the client requests a new copy of the
            /// mapping from the server.
            ///
            /// Uncached flow: <code>
            /// Client      |      Server
            /// | &lt;-------------- Hash |
            /// | Need Strings ------&gt; |
            /// | &lt;----------- Strings |
            /// | Dont Need Strings -&gt; |
            /// </code>
            ///
            /// Cached flow: <code>
            /// Client      |      Server
            /// | &lt;-------------- Hash |
            /// | Dont Need Strings -&gt; |
            /// </code>
            ///
            /// Verification failure flow: <code>
            /// Client      |      Server
            /// | &lt;-------------- Hash |
            /// | Need Strings ------&gt; |
            /// | &lt;----------- Strings |
            /// + Hash Failed          |
            /// | Need Strings ------&gt; |
            /// | &lt;----------- Strings |
            /// | Dont Need Strings -&gt; |
            ///  </code>
            ///
            /// NOTE: Verification failure flow is currently not implemented.
            /// </remarks>
            /// <param name="net">
            /// The <see cref="INetManager"/> to perform the protocol steps over.
            /// </param>
            /// <seealso cref="MsgServerHandshake"/>
            /// <seealso cref="MsgClientHandshake"/>
            /// <seealso cref="MsgStrings"/>
            /// <seealso cref="HandleServerHandshake"/>
            /// <seealso cref="HandleClientHandshake"/>
            /// <seealso cref="HandleStringsMessage"/>
            /// <seealso cref="OnClientCompleteHandshake"/>
            public static void NetworkInitialize(INetManager net)
            {
                _net = net;

                net.RegisterNetMessage<MsgServerHandshake>(
                    $"{nameof(RobustSerializer)}.{nameof(MappedStringSerializer)}.{nameof(MsgServerHandshake)}",
                    msg => HandleServerHandshake(net, msg));

                net.RegisterNetMessage<MsgClientHandshake>(
                    $"{nameof(RobustSerializer)}.{nameof(MappedStringSerializer)}.{nameof(MsgClientHandshake)}",
                    msg => HandleClientHandshake(net, msg));

                net.RegisterNetMessage<MsgStrings>(
                    $"{nameof(RobustSerializer)}.{nameof(MappedStringSerializer)}.{nameof(MsgStrings)}",
                    msg => HandleStringsMessage(net, msg));
            }

            /// <summary>
            /// Handles the reception, verification of a strings package
            /// and subsequent mapping of strings and initiator of
            /// receipt response.
            ///
            /// Uncached flow: <code>
            /// Client      |      Server
            /// | &lt;-------------- Hash |
            /// | Need Strings ------&gt; |
            /// | &lt;----------- Strings |
            /// | Dont Need Strings -&gt; | &lt;- you are here on client
            ///
            /// Verification failure flow: <code>
            /// Client      |      Server
            /// | &lt;-------------- Hash |
            /// | Need Strings ------&gt; |
            /// | &lt;----------- Strings |
            /// + Hash Failed          | &lt;- you are here on client
            /// | Need Strings ------&gt; |
            /// | &lt;----------- Strings |
            /// | Dont Need Strings -&gt; | &lt;- you are here on client
            ///  </code>
            ///
            /// NOTE: Verification failure flow is currently not implemented.
            /// </code>
            /// </summary>
            /// <exception cref="InvalidOperationException">Unable to verify strings package by hash.</exception>
            /// <seealso cref="NetworkInitialize"/>
            private static void HandleStringsMessage(INetManager net, MsgStrings msg)
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
                    // TODO: retry sending MsgClientHandshake with NeedsStrings = false
                    throw new InvalidOperationException("Unable to verify strings package by hash." + $"\n{ConvertToBase64Url(checkHash)} vs. {ConvertToBase64Url(ServerHash)}");
                }

                _stringMapHash = ServerHash;
                LockMappedStrings = true;

                LogSzr.Info($"Locked in at {_MappedStrings.Count} mapped strings.");

                WriteStringCache();

                // ok we're good now
                var channel = msg.MsgChannel;
                OnClientCompleteHandshake(net, channel);
            }

            /// <summary>
            /// Interpret a client's handshake, either sending a package
            /// of strings or completing the handshake.
            ///
            /// Uncached flow: <code>
            /// Client      |      Server
            /// | &lt;-------------- Hash |
            /// | Need Strings ------&gt; | &lt;- you are here on server
            /// | &lt;----------- Strings |
            /// | Dont Need Strings -&gt; | &lt;- you are here on server
            /// </code>
            ///
            /// Cached flow: <code>
            /// Client      |      Server
            /// | &lt;-------------- Hash |
            /// | Dont Need Strings -&gt; | &lt;- you are here on server
            /// </code>
            ///
            /// Verification failure flow: <code>
            /// Client      |      Server
            /// | &lt;-------------- Hash |
            /// | Need Strings ------&gt; | &lt;- you are here on server
            /// | &lt;----------- Strings |
            /// + Hash Failed          |
            /// | Need Strings ------&gt; | &lt;- you are here on server
            /// | &lt;----------- Strings |
            /// | Dont Need Strings -&gt; |
            ///  </code>
            ///
            /// NOTE: Verification failure flow is currently not implemented.
            /// </summary>
            /// <seealso cref="NetworkInitialize"/>
            private static void HandleClientHandshake(INetManager net, MsgClientHandshake msg)
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

                // TODO: count and limit number of requests to send strings during handshake

                var strings = msg.MsgChannel.NetPeer.CreateNetMessage<MsgStrings>();
                using (var ms = new MemoryStream())
                {
                    WriteStringPackage(ms);
                    ms.Position = 0;
                    strings.Package = ms.ToArray();
                    LogSzr.Info($"Sending {ms.Length} bytes sized mapped strings package to {msg.MsgChannel.RemoteEndPoint.Address}.");
                }

                msg.MsgChannel.SendMessage(strings);
            }

            /// <summary>
            /// Interpret a server's handshake, either requesting a package
            /// of strings or completing the handshake.
            ///
            /// Uncached flow: <code>
            /// Client      |      Server
            /// | &lt;-------------- Hash | &lt;- you are here on client
            /// | Need Strings ------&gt; |
            /// | &lt;----------- Strings |
            /// | Dont Need Strings -&gt; |
            /// </code>
            ///
            /// Cached flow: <code>
            /// Client      |      Server
            /// | &lt;-------------- Hash | &lt;- you are here on client
            /// | Dont Need Strings -&gt; |
            /// </code>
            ///
            /// Verification failure flow: <code>
            /// Client      |      Server
            /// | &lt;-------------- Hash | &lt;- you are here on client
            /// | Need Strings ------&gt; |
            /// | &lt;----------- Strings |
            /// + Hash Failed          |
            /// | Need Strings ------&gt; |
            /// | &lt;----------- Strings |
            /// | Dont Need Strings -&gt; |
            ///  </code>
            ///
            /// NOTE: Verification failure flow is currently not implemented.
            /// </summary>
            /// <exception cref="InvalidOperationException">Mapped strings are locked.</exception>
            /// <seealso cref="NetworkInitialize"/>
            private static void HandleServerHandshake(INetManager net, MsgServerHandshake msg)
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

                var fileName = CacheForHash(hashStr);
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
            }

            /// <summary>
            /// Inform the server that the client has a complete copy of the
            /// mapping, and alert other code that the handshake is over.
            /// </summary>
            /// <seealso cref="ClientHandshakeComplete"/>
            /// <seealso cref="NetworkInitialize"/>
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

            /// <summary>
            /// Gets the cache file associated with the given hash.
            /// </summary>
            /// <param name="hashStr">The hash to look up the cache for.</param>
            /// <returns>
            /// The filename where the cache for the given hash would be. The
            /// file itself may or may not exist. If it does not exist, no cache
            /// was made for the given hash.
            /// </returns>
            private static string CacheForHash(string hashStr)
                => PathHelpers.ExecutableRelativeFile($"strings-{hashStr}");

            /// <summary>
            ///  Saves the string cache to a file based on it's hash.
            /// </summary>
            private static void WriteStringCache()
            {
                var hashStr = Convert.ToBase64String(MappedStringsHash);
                hashStr = ConvertToBase64Url(hashStr);

                var fileName = CacheForHash(hashStr);
                using var file = File.OpenWrite(fileName);
                WriteStringPackage(file);

                LogSzr.Info($"Wrote string cache {hashStr}.");
            }

            private static byte[]? _mappedStringsPackage;

            private static byte[] MappedStringsPackage => LockMappedStrings
                ? _mappedStringsPackage ??= WriteStringPackage()
                : throw new InvalidOperationException("Mapped strings must be locked.");

            /// <summary>
            /// Writes strings to a package and converts to an array of bytes.
            /// </summary>
            /// <remarks>
            /// This is invoked by accessing <see cref="MappedStringsPackage"/> for the first time.
            /// </remarks>
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

            /// <summary>
            /// Writes a strings package to a stream.
            /// </summary>
            /// <param name="stream">A writable stream.</param>
            /// <exception cref="NotImplementedException">Overly long string in strings package.</exception>
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

            /// <summary>
            /// Loads a strings package from a stream.
            /// </summary>
            /// <remarks>
            /// Uses <see cref="ReadStringPackage"/> to extract strings and adds them to the mapping.
            /// </remarks>
            /// <param name="stream">A readable stream.</param>
            /// <returns>The number of strings loaded.</returns>
            /// <exception cref="InvalidOperationException">Mapped strings are locked, will not load.</exception>
            /// <exception cref="InvalidDataException">Did not read all bytes in package!</exception>
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
                        throw new InvalidDataException("Did not read all bytes in package!");
                    }
                }

                var added = MappedStrings.Count - started;
                return added;
            }

            /// <summary>
            /// Reads the contents of a strings package.
            /// </summary>
            /// <remarks>
            /// Does not add strings to the current mapping.
            /// </remarks>
            /// <param name="stream">A readable stream.</param>
            /// <returns>Strings from within the package.</returns>
            /// <exception cref="InvalidDataException">Could not read the full length of string #N.</exception>
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

            /// <summary>
            /// Converts a byte array such as a hash to a Base64 representation that is URL safe.
            /// </summary>
            /// <param name="data"></param>
            /// <returns>A base64url string form of the byte array.</returns>
            private static string ConvertToBase64Url(byte[]? data)
                => data == null ? "" : ConvertToBase64Url(Convert.ToBase64String(data));

            /// <summary>
            /// Converts a a Base64 string to one that is URL safe.
            /// </summary>
            /// <returns>A base64url formed string.</returns>
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

            /// <summary>
            /// Converts a URL-safe Base64 string into a byte array.
            /// </summary>
            /// <param name="s">A base64url formed string.</param>
            /// <returns>The represented byte array.</returns>
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

            /// <summary>
            /// Whether the string mapping is decided, and cannot be changed.
            /// </summary>
            /// <value>
            /// <para>
            /// While <c>false</c>, strings can be added to the mapping, but
            /// it cannot be saved to a cache.
            /// </para>
            /// <para>
            /// While <c>true</c>, the mapping cannot be modified, but can be
            /// shared between the server and client and saved to a cache.
            /// </para>
            /// </value>
            public static bool LockMappedStrings { get; set; }

            private static readonly Regex RxSymbolSplitter
                = new Regex(
                    @"(?<=[^\s\W])(?=[A-Z]) # Match for split at start of new capital letter
                            |(?<=[^0-9\s\W])(?=[0-9]) # Match for split before spans of numbers
                            |(?<=[A-Za-z0-9])(?=_) # Match for a split before an underscore
                            |(?=[.\\\/,#$?!@|&*()^`""'`~[\]{}:;\-]) # Match for a split after symbols
                            |(?<=[.\\\/,#$?!@|&*()^`""'`~[\]{}:;\-]) # Match for a split before symbols too",
                    RegexOptions.CultureInvariant
                    | RegexOptions.Compiled
                    | RegexOptions.IgnorePatternWhitespace
                );

            /// <summary>
            /// Add a string to the constant mapping.
            /// </summary>
            /// <remarks>
            /// If the string has multiple detectable subcomponents, such as a
            /// filepath, it may result in more than one string being added to
            /// the mapping. As string parts are commonly sent as subsets or
            /// scoped names, this increases the likelyhood of a successful
            /// string mapping.
            /// </remarks>
            /// <returns>
            /// <c>true</c> if the string was added to the mapping for the first
            /// time, <c>false</c> otherwise.
            /// </returns>
            /// <exception cref="InvalidOperationException">
            /// Thrown if the mapping is locked, and strings cannot be added, or
            /// if the string is not normalized (<see cref="String.IsNormalized()"/>).
            /// </exception>
            public static bool AddString(string str)
            {
                if (LockMappedStrings)
                {
                    if (_net.IsClient)
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

                if (str.Length <= MinMappedStringSize) return false;

                str = str.Trim();

                if (str.Length <= MinMappedStringSize) return false;

                str = str.Replace(Environment.NewLine, "\n");

                if (str.Length <= MinMappedStringSize) return false;

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

            /// <summary>
            /// Add the constant strings from an <see cref="Assembly"/> to the
            /// mapping.
            /// </summary>
            /// <param name="asm">The assembly from which to collect constant strings.</param>
            /// <exception cref="InvalidOperationException">
            /// Thrown if the mapping is locked.
            /// </exception>
            [MethodImpl(MethodImplOptions.Synchronized)]
            public static unsafe void AddStrings(Assembly asm)
            {
                if (LockMappedStrings)
                {
                    if (_net.IsClient)
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

            /// <summary>
            /// Add strings from the given <see cref="YamlStream"/> to the mapping.
            /// </summary>
            /// <remarks>
            /// Strings are taken from YAML anchors, tags, and leaf nodes.
            /// </remarks>
            /// <param name="yaml">The YAML to collect strings from.</param>
            /// <param name="name">The stream name. Only used for logging.</param>
            /// <exception cref="InvalidOperationException">
            /// Thrown if the mapping is locked.
            /// </exception>
            [MethodImpl(MethodImplOptions.Synchronized)]
            public static void AddStrings(YamlStream yaml, string name)
            {
                if (LockMappedStrings)
                {
                    if (_net.IsClient)
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

            /// <summary>
            /// Add strings from the given <see cref="JObject"/> to the mapping.
            /// </summary>
            /// <remarks>
            /// Strings are taken from JSON property names and string nodes.
            /// </remarks>
            /// <param name="obj">The JSON to collect strings from.</param>
            /// <param name="name">The stream name. Only used for logging.</param>
            /// <exception cref="InvalidOperationException">
            /// Thrown if the mapping is locked.
            /// </exception>
            public static void AddStrings(JObject obj, string name)
            {
                if (LockMappedStrings)
                {
                    if (_net.IsClient)
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

            /// <summary>
            /// Remove all strings from the mapping, completely resetting it.
            /// </summary>
            /// <exception cref="InvalidOperationException">
            /// Thrown if the mapping is locked.
            /// </exception>
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

            /// <summary>
            /// Add strings from the given enumeration to the mapping.
            /// </summary>
            /// <param name="strings">The strings to add.</param>
            /// <param name="providerName">The source provider of the strings to be logged.</param>
            /// <exception cref="InvalidOperationException">
            /// Thrown if the mapping is locked.
            /// </exception>
            [MethodImpl(MethodImplOptions.Synchronized)]
            public static void AddStrings(IEnumerable<string> strings, string providerName)
            {
                var started = MappedStrings.Count;
                foreach (var str in strings)
                {
                    AddString(str);
                }

                var added = MappedStrings.Count - started;
                LogSzr.Info($"Mapping {added} strings from {providerName}.");
            }

            private static byte[]? _stringMapHash;

            /// <value>
            /// The hash of the string mapping.
            /// </value>
            /// <exception cref="InvalidOperationException">
            /// Thrown if the mapping is not locked.
            /// </exception>
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

            /// <summary>
            /// Creates a SHA512 hash of the given array of bytes.
            /// </summary>
            /// <param name="data">An array of bytes to be hashed.</param>
            /// <returns>A 512-bit (64-byte) hash result as an array of bytes.</returns>
            /// <exception cref="ArgumentNullException"></exception>
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

            /// <summary>
            /// Implements <see cref="ITypeSerializer.Handles"/>.
            /// Specifies that this implementation handles strings.
            /// </summary>
            public bool Handles(Type type) => type == typeof(string);

            /// <summary>
            /// Implements <see cref="ITypeSerializer.GetSubtypes"/>.
            /// </summary>
            public IEnumerable<Type> GetSubtypes(Type type) => Type.EmptyTypes;

            /// <summary>
            /// Implements <see cref="IStaticTypeSerializer.GetStaticWriter"/>.
            /// </summary>
            /// <seealso cref="WriteMappedString"/>
            public MethodInfo GetStaticWriter(Type type) => WriteMappedStringMethodInfo;

            /// <summary>
            /// Implements <see cref="IStaticTypeSerializer.GetStaticReader"/>.
            /// </summary>
            /// <seealso cref="ReadMappedString"/>
            public MethodInfo GetStaticReader(Type type) => ReadMappedStringMethodInfo;

            private delegate void WriteStringDelegate(Stream stream, string? value);

            private delegate void ReadStringDelegate(Stream stream, out string? value);

            private static readonly MethodInfo WriteMappedStringMethodInfo
                = ((WriteStringDelegate) WriteMappedString).Method;

            private static readonly MethodInfo ReadMappedStringMethodInfo
                = ((ReadStringDelegate) ReadMappedString).Method;

            private static readonly char[] TrimmableSymbolChars =
            {
                '.', '\\', '/', ',', '#', '$', '?', '!', '@', '|', '&', '*', '(', ')', '^', '`', '"', '\'', '`', '~', '[', ']', '{', '}', ':', ';', '-'
            };

            /// <summary>
            /// The shortest a string can be in order to be inserted in the mapping.
            /// </summary>
            /// <remarks>
            /// Strings below a certain length aren't worth compressing.
            /// </remarks>
            private const int MinMappedStringSize = 3;

            /// <summary>
            /// The longest a string can be in order to be inserted in the mapping.
            /// </summary>
            private const int MaxMappedStringSize = 420;

            /// <summary>
            /// The special value corresponding to a <c>null</c> string in the
            /// encoding.
            /// </summary>
            private const int MappedNull = 0;

            /// <summary>
            /// The special value corresponding to a string which was not mapped.
            /// This is followed by the bytes of the unmapped string.
            /// </summary>
            private const int UnmappedString = 1;

            /// <summary>
            /// The first non-special value, used for encoding mapped strings.
            /// </summary>
            /// <remarks>
            /// Since previous values are taken by <see cref="MappedNull"/> and
            /// <see cref="UnmappedString"/>, this value is used to encode
            /// mapped strings at an offset - in the encoding, a value
            /// <c>>= FirstMappedIndexStart</c> represents the string with
            /// mapping of that value <c> - FirstMappedIndexStart</c>.
            /// </remarks>
            private const int FirstMappedIndexStart = 2;

            /// <summary>
            /// Write the encoding of the given string to the stream.
            /// </summary>
            /// <param name="stream">The stream to write to.</param>
            /// <param name="value"> The (possibly null) string to write.</param>
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

            /// <summary>
            /// Try to read a string from the given stream..
            /// </summary>
            /// <param name="stream">The stream to read from.</param>
            /// <param name="value"> The (possibly null) string read.</param>
            /// <exception cref="InvalidOperationException">
            /// Thrown if the mapping is not locked.
            /// </exception>
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

            /// <summary>
            /// See <see cref="OnClientCompleteHandshake"/>.
            /// </summary>
            public static event Action? ClientHandshakeComplete;

        }

    }

}
