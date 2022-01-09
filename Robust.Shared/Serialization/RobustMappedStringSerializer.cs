using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NetSerializer;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;
using static Robust.Shared.Utility.Base64Helpers;

namespace Robust.Shared.Serialization
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
    internal sealed partial class RobustMappedStringSerializer : IStaticTypeSerializer, IRobustMappedStringSerializer
    {
        private delegate void WriteStringDelegate(Stream stream, string? value);

        private delegate void ReadStringDelegate(Stream stream, out string? value);

        private static MethodInfo WriteMappedStringMethodInfo
            => ((WriteStringDelegate) StaticWriteMappedString).Method;

        private static MethodInfo ReadMappedStringMethodInfo
            => ((ReadStringDelegate) StaticReadMappedString).Method;

        private static readonly char[] TrimmableSymbolChars =
        {
            '.', '\\', '/', ',', '#', '$', '?', '!', '@', '|', '&',
            '*', '(', ')', '^', '`', '"', '\'', '`', '~', '[', ']',
            '{', '}', ':', ';', '-'
        };

        private static readonly HashAlgorithmName PackHashAlgo = HashAlgorithmName.SHA512;

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
        private const uint MappedNull = 0;

        /// <summary>
        /// The special value corresponding to a string which was not mapped.
        /// This is followed by the bytes of the unmapped string.
        /// </summary>
        private const uint UnmappedString = 1;

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
        private const uint FirstMappedIndexStart = 2;

        [Dependency] private readonly INetManager _net = default!;

        // I don't want to create 50 line changes in this commit so...
        // ReSharper disable once InconsistentNaming
        private ISawmill LogSzr = default!;

        private MappedStringDict _dict = default!;

        private readonly Dictionary<INetChannel, InProgressHandshake> _incompleteHandshakes
            = new();

        private byte[]? _mappedStringsPackage;
        private byte[]? _serverHash;
        private byte[]? _stringMapHash;

        /// <value>
        /// The hash of the string mapping.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the mapping is not locked.
        /// </exception>
        public ReadOnlySpan<byte> MappedStringsHash => _stringMapHash;

        public bool EnableCaching { get; set; } = true;

        private static readonly Regex RxSymbolSplitter
            = new(
                @"(?<=[^\s\W])(?=[A-Z]) # Match for split at start of new capital letter
                            |(?<=[^0-9\s\W])(?=[0-9]) # Match for split before spans of numbers
                            |(?<=[A-Za-z0-9])(?=_) # Match for a split before an underscore
                            |(?=[.\\\/,#$?!@|&*()^`""'`~[\]{}:;\-]) # Match for a split after symbols
                            |(?<=[.\\\/,#$?!@|&*()^`""'`~[\]{}:;\-]) # Match for a split before symbols too",
                RegexOptions.CultureInvariant
                | RegexOptions.Compiled
                | RegexOptions.IgnorePatternWhitespace
            );

        public bool Locked => _dict.Locked;

        public ITypeSerializer TypeSerializer => this;

        /// <summary>
        /// Starts the handshake from the server end of the given channel,
        /// sending a <see cref="MsgMapStrServerHandshake"/>.
        /// </summary>
        /// <param name="channel">The network channel to perform the handshake over.</param>
        /// <remarks>
        /// Locks the string mapping if this is the first time the server is
        /// performing the handshake.
        /// </remarks>
        /// <seealso cref="MsgMapStrClientHandshake"/>
        /// <seealso cref="MsgMapStrStrings"/>
        public Task Handshake(INetChannel channel)
        {
            DebugTools.Assert(_net.IsServer);
            DebugTools.Assert(_dict.Locked);

            var tcs = new TaskCompletionSource<object?>();

            _incompleteHandshakes.Add(channel, new InProgressHandshake(tcs));

            var message = _net.CreateNetMessage<MsgMapStrServerHandshake>();
            message.Hash = _stringMapHash;
            _net.ServerSendMessage(message, channel);

            return tcs.Task;
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
        /// <seealso cref="MsgMapStrServerHandshake"/>
        /// <seealso cref="MsgMapStrClientHandshake"/>
        /// <seealso cref="MsgMapStrStrings"/>
        /// <seealso cref="HandleServerHandshake"/>
        /// <seealso cref="HandleClientHandshake"/>
        /// <seealso cref="HandleStringsMessage"/>
        /// <seealso cref="OnClientCompleteHandshake"/>
        private void NetworkInitialize()
        {
            _net.RegisterNetMessage<MsgMapStrServerHandshake>(HandleServerHandshake, NetMessageAccept.Client | NetMessageAccept.Handshake);
            _net.RegisterNetMessage<MsgMapStrClientHandshake>(HandleClientHandshake, NetMessageAccept.Server | NetMessageAccept.Handshake);
            _net.RegisterNetMessage<MsgMapStrStrings>(HandleStringsMessage, NetMessageAccept.Client | NetMessageAccept.Handshake);

            _net.Disconnect += NetOnDisconnect;
        }

        private void NetOnDisconnect(object? sender, NetDisconnectedArgs e)
        {
            // Cancel handshake in-progress if client disconnects mid-handshake.
            var channel = e.Channel;
            if (_incompleteHandshakes.TryGetValue(channel, out var handshake))
            {
                var tcs = handshake.Tcs;
                LogSzr.Debug($"Cancelling handshake for disconnected client {channel.UserId}");
                tcs.SetCanceled();
            }

            _incompleteHandshakes.Remove(channel);
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
        private void HandleStringsMessage(MsgMapStrStrings msg)
        {
            DebugTools.Assert(_net.IsClient);
            DebugTools.AssertNotNull(msg.Package);
            DebugTools.AssertNotNull(_serverHash);

            var packageStream = new MemoryStream(msg.Package!, false);
            _dict.LoadFromPackage(packageStream, out var hash);

            if (!hash.SequenceEqual(_serverHash!))
            {
                // TODO: retry sending MsgClientHandshake with NeedsStrings = false
                throw new InvalidOperationException("Unable to verify strings package by hash." +
                                                    $"\n{ConvertToBase64Url(hash)} vs. {ConvertToBase64Url(_serverHash)}");
            }

            _stringMapHash = _serverHash;

            LogSzr.Debug($"Locked in at {_dict.StringCount} mapped strings.");

            packageStream.Position = 0;
            if (EnableCaching)
            {
                WriteStringCache(packageStream);
            }

            // ok we're good now
            var channel = msg.MsgChannel;
            OnClientCompleteHandshake(_net, channel);
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
        private void HandleClientHandshake(MsgMapStrClientHandshake msgMapStr)
        {
            DebugTools.Assert(_net.IsServer);
            DebugTools.Assert(_dict.Locked);

            var channel = msgMapStr.MsgChannel;
            LogSzr.Debug($"Received handshake from {channel.UserName}.");

            if (!_incompleteHandshakes.TryGetValue(channel, out var handshake))
            {
                channel.Disconnect("MsgMapStrClientHandshake without in-progress handshake.");
                return;
            }

            if (!msgMapStr.NeedsStrings)
            {
                LogSzr.Debug($"Completing handshake with {channel.UserName}.");

                handshake.Tcs.SetResult(null);
                _incompleteHandshakes.Remove(channel);
                return;
            }

            if (handshake.HasRequestedStrings)
            {
                channel.Disconnect("Cannot request strings twice");
                return;
            }

            handshake.HasRequestedStrings = true;

            var strings = _net.CreateNetMessage<MsgMapStrStrings>();
            strings.Package = _mappedStringsPackage;
            LogSzr.Debug(
                $"Sending {_mappedStringsPackage!.Length} bytes sized mapped strings package to {channel.UserName}.");

            _net.ServerSendMessage(strings, channel);
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
        private void HandleServerHandshake(MsgMapStrServerHandshake msgMapStr)
        {
            DebugTools.Assert(_net.IsClient);

            _serverHash = msgMapStr.Hash;

            var hashStr = ConvertToBase64Url(msgMapStr.Hash!);

            LogSzr.Debug($"Received server handshake with hash {hashStr}.");

            var fileName = CacheForHash(hashStr);
            if (fileName == null || !File.Exists(fileName))
            {
                LogSzr.Debug($"No string cache for {hashStr}.");
                var handshake = _net.CreateNetMessage<MsgMapStrClientHandshake>();
                LogSzr.Debug("Asking server to send mapped strings.");
                handshake.NeedsStrings = true;
                msgMapStr.MsgChannel.SendMessage(handshake);
            }
            else
            {
                LogSzr.Debug($"We had a cached string map that matches {hashStr}.");
                using var file = File.OpenRead(fileName);
                var added = _dict.LoadFromPackage(file, out _);

                _stringMapHash = msgMapStr.Hash!;
                LogSzr.Debug($"Read {added} strings from cache {hashStr}.");
                LogSzr.Debug($"Locked in at {_dict.StringCount} mapped strings.");
                // ok we're good now
                var channel = msgMapStr.MsgChannel;
                OnClientCompleteHandshake(_net, channel);
            }
        }

        /// <summary>
        /// Inform the server that the client has a complete copy of the
        /// mapping, and alert other code that the handshake is over.
        /// </summary>
        /// <seealso cref="ClientHandshakeComplete"/>
        /// <seealso cref="NetworkInitialize"/>
        private void OnClientCompleteHandshake(INetManager net, INetChannel channel)
        {
            LogSzr.Debug("Letting server know we're good to go.");
            var handshake = net.CreateNetMessage<MsgMapStrClientHandshake>();
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
        private string? CacheForHash(string hashStr)
        {
            if (!EnableCaching)
            {
                return null;
            }
            return PathHelpers.ExecutableRelativeFile($"strings-{hashStr}");
        }

        /// <summary>
        ///  Saves the string cache to a file based on it's hash.
        /// </summary>
        private void WriteStringCache(Stream stream)
        {
            var hashStr = Convert.ToBase64String(MappedStringsHash);
            hashStr = ConvertToBase64Url(hashStr);

            var fileName = CacheForHash(hashStr)!;
            using var file = File.OpenWrite(fileName);
            stream.CopyTo(file);

            LogSzr.Debug($"Wrote string cache {hashStr}.");
        }

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
        /// Thrown if the string is not normalized (<see cref="String.IsNormalized()"/>).
        /// </exception>
        public void AddString(string str)
        {
            if (!_net.IsClient)
            {
                _dict.AddString(str);
            }
        }

        /// <summary>
        /// Add the constant strings from an <see cref="Assembly"/> to the
        /// mapping.
        /// </summary>
        /// <param name="asm">The assembly from which to collect constant strings.</param>
        public void AddStrings(Assembly asm)
        {
            if (!_net.IsClient)
            {
                _dict.AddStrings(asm);
            }
        }

        /// <summary>
        /// Add strings from the given <see cref="YamlStream"/> to the mapping.
        /// </summary>
        /// <remarks>
        /// Strings are taken from YAML anchors, tags, and leaf nodes.
        /// </remarks>
        /// <param name="yaml">The YAML to collect strings from.</param>
        public void AddStrings(YamlStream yaml)
        {
            if (!_net.IsClient)
            {
                _dict.AddStrings(yaml);
            }
        }

        /// <summary>
        /// Add strings from the given enumeration to the mapping.
        /// </summary>
        /// <param name="strings">The strings to add.</param>
        public void AddStrings(IEnumerable<string> strings)
        {
            if (!_net.IsClient)
            {
                _dict.AddStrings(strings);
            }
        }

        /// <summary>
        /// Implements <see cref="ITypeSerializer.Handles"/>.
        /// Specifies that this implementation handles strings.
        /// </summary>
        bool ITypeSerializer.Handles(Type type) => type == typeof(string);

        /// <summary>
        /// Implements <see cref="ITypeSerializer.GetSubtypes"/>.
        /// </summary>
        IEnumerable<Type> ITypeSerializer.GetSubtypes(Type type) => Type.EmptyTypes;

        /// <summary>
        /// Implements <see cref="IStaticTypeSerializer.GetStaticWriter"/>.
        /// </summary>
        /// <seealso cref="WriteMappedString"/>
        MethodInfo IStaticTypeSerializer.GetStaticWriter(Type type)
        {
            return WriteMappedStringMethodInfo;
        }

        /// <summary>
        /// Implements <see cref="IStaticTypeSerializer.GetStaticReader"/>.
        /// </summary>
        /// <seealso cref="ReadMappedString"/>
        MethodInfo IStaticTypeSerializer.GetStaticReader(Type type)
        {
            return ReadMappedStringMethodInfo;
        }

        /// <summary>
        /// Write the encoding of the given string to the stream.
        /// Static form of <see cref="WriteMappedString"/> for use by <see cref="NetSerializer"/>.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="value"> The (possibly null) string to write.</param>
        private static void StaticWriteMappedString(Stream stream, string? value)
        {
            var mss = (RobustMappedStringSerializer) IoCManager.Resolve<IRobustMappedStringSerializer>();
            mss._dict.WriteMappedString(stream, value);
        }
        /// <summary>
        /// Try to read a string from the given stream.
        /// Static form of <see cref="ReadMappedString"/> for use by <see cref="NetSerializer"/>.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="value"> The (possibly null) string read.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the mapping is not locked.
        /// </exception>
        private static void StaticReadMappedString(Stream stream, out string? value)
        {
            var mss = (RobustMappedStringSerializer) IoCManager.Resolve<IRobustMappedStringSerializer>();
            mss._dict.ReadMappedString(stream, out value);
        }

        /// <summary>
        /// See <see cref="OnClientCompleteHandshake"/>.
        /// </summary>
        public event Action? ClientHandshakeComplete;

        public void LockStrings()
        {
            var sw = Stopwatch.StartNew();
            _dict.FinalizeMapping();

            LogSzr.Debug($"Finalized string mapping of size {_dict.StringCount} in {sw.ElapsedMilliseconds}ms");
            sw.Restart();

            (_stringMapHash, _mappedStringsPackage) = _dict.GeneratePackage();

            LogSzr.Debug($"Wrote string package in {sw.ElapsedMilliseconds}ms size {ByteHelpers.FormatBytes(_mappedStringsPackage.Length)}");
            LogSzr.Debug($"String hash is {ConvertToBase64Url(_stringMapHash)}");

            // File.WriteAllText("strings.txt", string.Join("\n", _dict._mappedStrings!));
        }

        public void Initialize()
        {
            LogSzr = Logger.GetSawmill("szr");
            _dict = new MappedStringDict(LogSzr);

            if (_net.IsClient)
            {
                // Client cannot make its own string dictionary, lock immediately.
                _dict.Locked = true;
            }

            NetworkInitialize();
        }

        private sealed class InProgressHandshake
        {
            public readonly TaskCompletionSource<object?> Tcs;
            public bool HasRequestedStrings;

            public InProgressHandshake(TaskCompletionSource<object?> tcs)
            {
                Tcs = tcs;
            }
        }
    }
}
