using ServerInterfaces.Player;
using ServerInterfaces.GameMode;
using ServerInterfaces.Round;

namespace SS13_Server.Modules
{
    class RoundManager : IRoundManager
    {
        public IGameMode CurrentGameMode { get; private set; }

        private bool _ready;

        public void Initialize(IGameMode gamemode) //Called by StartLobby() before InitModules.
        {
            CurrentGameMode = gamemode;
            _ready = true;
        }

        public void SpawnPlayer(IPlayerSession player)
        {
            if (!_ready) return;
            CurrentGameMode.SpawnPlayer(player);
            CurrentGameMode.PlayerJoined(player);
        }
    }
}
