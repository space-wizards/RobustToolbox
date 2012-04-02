using ServerInterfaces.GameMode;
using ServerInterfaces.Player;

namespace ServerInterfaces.Round
{
    public interface IRoundManager
    {
        IGameMode CurrentGameMode { get; }

        void Initialize(IGameMode gamemode);

        void SpawnPlayer(IPlayerSession player);
    }
}
