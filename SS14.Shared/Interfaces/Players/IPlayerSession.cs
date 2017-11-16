using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Interfaces.Players
{
    public interface IPlayerSession
    {
        INetChannel NetChannel { get; }

        int Entity { get; }
    }
}
