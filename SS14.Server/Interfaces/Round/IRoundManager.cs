using SS14.Server.Interfaces.GameMode;
using SS14.Server.Interfaces.Player;
using SS14.Shared.IoC;

namespace SS14.Server.Interfaces.Round
{
    public interface IRoundManager
    {
        IGameMode CurrentGameMode { get; }

        void Initialize(IGameMode gamemode);

        void SpawnPlayer(IPlayerSession player);
    }
}
