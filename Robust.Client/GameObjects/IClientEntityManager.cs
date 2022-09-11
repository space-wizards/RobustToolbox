using Robust.Shared.GameObjects;

namespace Robust.Client.GameObjects
{
    public interface IClientEntityManager : IEntityManager, IEntityNetworkManager
    {
        /// <summary>
        ///     Sends a networked message to the server, while also repeatedly raising it locally for every time this tick gets re-predicted.
        /// </summary>
        void RaisePredictiveEvent<T>(T msg) where T : EntityEventArgs;
    }
}
