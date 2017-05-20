using SS14.Server.Interfaces.GameMode;
using SS14.Server.Interfaces.Player;
using SS14.Server.Interfaces.Round;
using SS14.Shared.IoC;

namespace SS14.Server.Services.Round
{
    [IoCTarget]
    internal class RoundManager : IRoundManager
    {
        private bool _ready;

        #region IRoundManager Members

        public IGameMode CurrentGameMode { get; private set; }

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

        #endregion
    }
}