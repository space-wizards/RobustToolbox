using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Server.Modules.Gamemodes;
using SS13_Server.Modules;
using SS13_Server;

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

        private IGameMode currentGameMode;

        public IGameMode CurrentGameMode { get { return currentGameMode; } private set { currentGameMode = value; } }

        private bool ready = false;

        public void Initialize(IGameMode gamemode) //Called by StartLobby() before InitModules.
        {
            currentGameMode = gamemode;
            ready = true;
        }

        public void SpawnPlayer(PlayerSession player)
        {
            if (!ready) return;
            currentGameMode.SpawnPlayer(player);
            currentGameMode.PlayerJoined(player);
        }
    }
}
