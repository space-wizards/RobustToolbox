using SS13_Server.Modules.Gamemodes;

namespace SS13_Server.Modules
{
    class RoundManager
    {
        #region Singleton
        static readonly RoundManager singleton = new RoundManager();

        static RoundManager()
        {
        }

        RoundManager()
        {
        }

        public static RoundManager Singleton
        {
            get
            {
                return singleton;
            }
        } 
        #endregion

        public IGameMode CurrentGameMode { get; private set; }

        private bool _ready;

        public void Initialize(IGameMode gamemode) //Called by StartLobby() before InitModules.
        {
            CurrentGameMode = gamemode;
            _ready = true;
        }

        public void SpawnPlayer(PlayerSession player)
        {
            if (!_ready) return;
            CurrentGameMode.SpawnPlayer(player);
            CurrentGameMode.PlayerJoined(player);
        }
    }
}
