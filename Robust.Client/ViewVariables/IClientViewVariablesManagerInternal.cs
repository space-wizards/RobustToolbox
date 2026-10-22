using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Robust.Client.ViewVariables.Instances;
using Robust.Shared.Network.Messages;
using Robust.Shared.ViewVariables;

namespace Robust.Client.ViewVariables
{
    internal interface IClientViewVariablesManagerInternal : IClientViewVariablesManager
    {
        void Initialize();

        /// <summary>
        ///     Creates the ideal property editor for a specific property type.
        /// </summary>
        /// <param name="type">The type of the property to create an editor for.</param>
        VVPropEditor PropertyFor(Type? type);

        /// <summary>
        ///     Requests a session to an object on the server.
        /// </summary>
        /// <param name="selector">The selector so the server knows what object we want.</param>
        /// <returns>A session that can be used to request data and modify the remote object.</returns>
        Task<ViewVariablesRemoteSession> RequestSession(ViewVariablesObjectSelector selector);

        /// <summary>
        ///     Requests a data blob from the object referenced by a VV session.
        /// </summary>
        /// <param name="session">The session for the remote object.</param>
        /// <param name="meta">A request object the server uses to know what kind of data you want of the remote object.</param>
        Task<ViewVariablesBlob> RequestData(ViewVariablesRemoteSession session, ViewVariablesRequest meta);

        /// <summary>
        ///     Requests a data blob from the object referenced by a VV session.
        /// </summary>
        /// <typeparam name="T">The type of blob that is expected of the server to be sent back, to be automatically cast for convenience.</typeparam>
        /// <param name="session">The session for the remote object.</param>
        /// <param name="meta">A request object the server uses to know what kind of data you want of the remote object.</param>
        Task<T> RequestData<T>(ViewVariablesRemoteSession session, ViewVariablesRequest meta) where T : ViewVariablesBlob;

        /// <summary>
        ///     Close a session to a remote object.
        /// </summary>
        /// <param name="session">The session to close.</param>
        void CloseSession(ViewVariablesRemoteSession session);

        /// <summary>
        ///     Attempts to get a VV session given its Uid.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        bool TryGetSession(uint sessionId, [NotNullWhen(true)] out ViewVariablesRemoteSession? session);

        /// <summary>
        ///     Modify a remote object.
        /// </summary>
        /// <param name="session">The session pointing to the remote object.</param>
        /// <param name="propertyIndex">An array of objects that the server can parse to figure out what to assign.</param>
        /// <param name="value">The new value for the object.</param>
        /// <param name="reinterpretValue">Whether the <see cref="value"/> will be reinterpreted on the server. Also see: <seealso cref="MsgViewVariablesModifyRemote.ReinterpretValue"/></param>
        void ModifyRemote(ViewVariablesRemoteSession session, object[] propertyIndex, object? value, bool reinterpretValue = false);

        /// <summary>
        ///     Gets a collection of trait IDs that are agreed upon so <see cref="ViewVariablesInstanceObject"/> knows which traits to instantiate.
        /// </summary>
        /// <seealso cref="ViewVariablesBlobMetadata.Traits" />
        /// <seealso cref="Shared.ViewVariables.ViewVariablesManager.TraitIdsFor"/>
        ICollection<object> TraitIdsFor(Type type);

        /// <summary>
        ///     Load the DocStrings for use by VV
        /// </summary>
        /// <remarks>
        ///     The other DocString functions lazily call this if it hasn't been called yet.
        /// </remarks>
        void LoadDocStrings();

        /// <summary>
        ///     Retrieve a DocString for the given type definition
        /// </summary>
        /// <param name="type"></param>
        /// <returns>DocString for the type or an error string</returns>
        string GetDocStringForType(Type type);

        /// <summary>
        ///     Retrieve a DocString for a type definition matching the given key
        /// </summary>
        /// <param name="key"></param>
        /// <returns>DocString for the type or an error string</returns>
        string GetDocStringForType(string key);

        /// <summary>
        ///     Retrieve a DocString for a field or property definition matching the given key
        /// </summary>
        /// <param name="key"></param>
        /// <returns>DocString for the field or property or an error string</returns>
        string GetDocStringForFieldOrProperty(string key);

        /// <summary>
        ///     Retrieve DocStrings for many keys at once, asking the server for any we don't have locally.
        /// </summary>
        /// <remarks>
        ///     Server-only code has its docs in a folder clients can't see, so remote objects need this.
        ///     Server answers are cached until disconnect.
        /// </remarks>
        /// <returns>One DocString or error string per key, in the same order as <paramref name="keys"/></returns>
        Task<string[]> GetDocStringsAsync(ViewVariablesDocStringKind kind, IReadOnlyList<string> keys);

        /// <summary>
        ///     Fire-and-forget version of <see cref="GetDocStringsAsync"/> that hands each result to its callback.
        /// </summary>
        void ApplyDocStringsAsync(ViewVariablesDocStringKind kind, IReadOnlyList<(string Key, Action<string> Apply)> targets);
    }
}
