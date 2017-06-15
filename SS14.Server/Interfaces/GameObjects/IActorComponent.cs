using SS14.Server.Interfaces.Player;

namespace SS14.Server.Interfaces.GameObjects
{
    public interface IActorComponent
    {
        IPlayerSession playerSession { get; }
    }
}
