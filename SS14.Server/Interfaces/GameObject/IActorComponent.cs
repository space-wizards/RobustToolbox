using SS14.Server.Interfaces.Player;

namespace SS14.Server.Interfaces.GOC
{
    public interface IActorComponent
    {
        IPlayerSession playerSession { get; }
    }
}
