using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Network.Messages;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization
{
    [PublicAPI]
    internal interface IRobustMappedStringSerializer
    {
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
        Task Handshake(INetChannel channel);

        /// <value>
        /// The hash of the string mapping.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the mapping is not locked.
        /// </exception>
        ReadOnlySpan<byte> MappedStringsHash { get; }

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
        void AddString(string str);

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
        /// Add strings from the given enumeration to the mapping.
        /// </summary>
        /// <param name="strings">The strings to add.</param>
        /// <param name="providerName">The source provider of the strings to be logged.</param>
        void AddStrings(IEnumerable<string> strings, string providerName);

        /// <summary>
        /// See <see cref="RobustMappedStringSerializer.OnClientCompleteHandshake"/>.
        /// </summary>
        event Action? ClientHandshakeComplete;

        void LockStrings();
    }
}
