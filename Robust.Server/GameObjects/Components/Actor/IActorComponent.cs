using Robust.Server.Player;
using Robust.Shared.GameObjects;

namespace Robust.Server.GameObjects
{
    public interface IActorComponent : IComponent
    {
        IPlayerSession playerSession { get; }
    }
}
