using Robust.Shared.GameObjects;

namespace Robust.Client.GameObjects
{
    public interface IClientEntityManager : IEntityManager, IEntityNetworkManager
    {
        /// <summary>
        ///     Raises a networked message as if it had arrived from the sever.
        /// </summary>
        public void DispatchReceivedNetworkMsg(EntityEventArgs msg);
    }
}
