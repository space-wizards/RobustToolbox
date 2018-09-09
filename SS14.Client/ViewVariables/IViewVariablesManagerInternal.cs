using System;
using System.Threading.Tasks;
using SS14.Shared.ViewVariables;

namespace SS14.Client.ViewVariables
{
    internal interface IViewVariablesManagerInternal : IViewVariablesManager
    {
        void Initialize();

        /// <summary>
        ///     Creates the ideal property editor for a specific property type.
        /// </summary>
        /// <param name="type">The type of the property to create an editor for.</param>
        ViewVariablesPropertyEditor PropertyFor(Type type);

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
        Task<ViewVariablesBlob> RequestData(ViewVariablesRemoteSession session);

        /// <summary>
        ///     Close a session to a remote object.
        /// </summary>
        /// <param name="session">The session to close.</param>
        void CloseSession(ViewVariablesRemoteSession session);

        /// <summary>
        ///     Modify a remote object.
        /// </summary>
        /// <param name="session">The session pointing to the remote object.</param>
        /// <param name="propertyName">The name of the property to modify.</param>
        /// <param name="value">The new value for the object.</param>
        void ModifyRemote(ViewVariablesRemoteSession session, string propertyName, object value);
    }
}
