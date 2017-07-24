using SS14.Server.Interfaces.Player;
using SS14.Shared.Interfaces.GameObjects;

namespace SS14.Server.Interfaces.GameObjects
{
    public interface IActorComponent : IComponent
    {
        IPlayerSession playerSession { get; }
    }
}
