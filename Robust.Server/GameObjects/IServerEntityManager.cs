using Robust.Shared.GameObjects;

namespace Robust.Server.GameObjects
{
    /// <summary>
    /// Server side version of the <see cref="IEntityManager"/>.
    /// </summary>
    public interface IServerEntityManager : IEntityManager, IServerEntityNetworkManager { }
}
