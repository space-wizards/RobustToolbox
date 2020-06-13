using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NetSerializer;
using Newtonsoft.Json.Linq;
using Robust.Shared.Interfaces.Network;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization
{

    [PublicAPI]
    public interface IRobustMappedStringSerializer
    {

        /// <summary>
        /// Starts the handshake from the server end of the given channel,
        /// sending a <see cref="MsgRobustMappedStringsSerializerServerHandshake"/>.
        /// </summary>
        /// <param name="channel">The network channel to perform the handshake over.</param>
        /// <remarks>
        /// Locks the string mapping if this is the first time the server is
        /// performing the handshake.
        /// </remarks>
        /// <seealso cref="MsgRobustMappedStringsSerializerClientHandshake"/>
        /// <seealso cref="MsgRobustMappedStringsSerializerStrings"/>
        Task Handshake(INetChannel channel);

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
        /// <seealso cref="MsgRobustMappedStringsSerializerServerHandshake"/>
        /// <seealso cref="MsgRobustMappedStringsSerializerClientHandshake"/>
        /// <seealso cref="MsgRobustMappedStringsSerializerStrings"/>
        /// <seealso cref="RobustMappedStringSerializer.HandleServerHandshake"/>
        /// <seealso cref="RobustMappedStringSerializer.HandleClientHandshake"/>
        /// <seealso cref="RobustMappedStringSerializer.HandleStringsMessage"/>
        /// <seealso cref="RobustMappedStringSerializer.OnClientCompleteHandshake"/>
        void NetworkInitialize(INetManager net);

        /// <summary>
        /// Writes a strings package to a stream.
        /// </summary>
        /// <param name="stream">A writable stream.</param>
        /// <exception cref="NotImplementedException">Overly long string in strings package.</exception>
        void WriteStringPackage(Stream stream);

        /// <summary>
        /// Converts a URL-safe Base64 string into a byte array.
        /// </summary>
        /// <param name="s">A base64url formed string.</param>
        /// <returns>The represented byte array.</returns>
        byte[] ConvertFromBase64Url(string s);

        IReadOnlyList<String> MappedStrings { get; }

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
        bool LockMappedStrings { get; set; }

        /// <value>
        /// The hash of the string mapping.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the mapping is not locked.
        /// </exception>
        byte[] MappedStringsHash { get; }

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
        bool AddString(string str);

        /// <summary>
        /// Add the constant strings from an <see cref="Assembly"/> to the
        /// mapping.
        /// </summary>
        /// <param name="asm">The assembly from which to collect constant strings.</param>
        void AddStrings(Assembly asm);

        /// <summary>
        /// Add strings from the given <see cref="YamlStream"/> to the mapping.
        /// </summary>
        /// <remarks>
        /// Strings are taken from YAML anchors, tags, and leaf nodes.
        /// </remarks>
        /// <param name="yaml">The YAML to collect strings from.</param>
        /// <param name="name">The stream name. Only used for logging.</param>
        void AddStrings(YamlStream yaml, string name);

        /// <summary>
        /// Add strings from the given <see cref="JObject"/> to the mapping.
        /// </summary>
        /// <remarks>
        /// Strings are taken from JSON property names and string nodes.
        /// </remarks>
        /// <param name="obj">The JSON to collect strings from.</param>
        /// <param name="name">The stream name. Only used for logging.</param>
        void AddStrings(JObject obj, string name);

        /// <summary>
        /// Remove all strings from the mapping, completely resetting it.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the mapping is locked.
        /// </exception>
        void ClearStrings();

        /// <summary>
        /// Add strings from the given enumeration to the mapping.
        /// </summary>
        /// <param name="strings">The strings to add.</param>
        /// <param name="providerName">The source provider of the strings to be logged.</param>
        void AddStrings(IEnumerable<string> strings, string providerName);

        /// <summary>
        /// Implements <see cref="ITypeSerializer.Handles"/>.
        /// Specifies that this implementation handles strings.
        /// </summary>
        bool Handles(Type type);

        /// <summary>
        /// Implements <see cref="ITypeSerializer.GetSubtypes"/>.
        /// </summary>
        IEnumerable<Type> GetSubtypes(Type type);

        /// <summary>
        /// Implements <see cref="IStaticTypeSerializer.GetStaticWriter"/>.
        /// </summary>
        /// <seealso cref="RobustMappedStringSerializer.WriteMappedString"/>
        MethodInfo GetStaticWriter(Type type);

        /// <summary>
        /// Implements <see cref="IStaticTypeSerializer.GetStaticReader"/>.
        /// </summary>
        /// <seealso cref="RobustMappedStringSerializer.ReadMappedString"/>
        MethodInfo GetStaticReader(Type type);

        /// <summary>
        /// Write the encoding of the given string to the stream.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="value"> The (possibly null) string to write.</param>
        void WriteMappedString(Stream stream, string? value);

        /// <summary>
        /// Try to read a string from the given stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="value"> The (possibly null) string read.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the mapping is not locked.
        /// </exception>
        void ReadMappedString(Stream stream, out string? value);

        /// <summary>
        /// See <see cref="RobustMappedStringSerializer.OnClientCompleteHandshake"/>.
        /// </summary>
        event Action? ClientHandshakeComplete;

    }

}
