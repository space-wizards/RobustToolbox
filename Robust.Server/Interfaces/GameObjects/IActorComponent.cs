using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.GameObjects;

namespace Robust.Server.Interfaces.GameObjects
{
    public interface IActorComponent : IComponent
    {
        IPlayerSession playerSession { get; }
    }
}
