using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared;
using SS13_Shared.ServerEnums;

namespace ServerServices.Configuration
{
    [Serializable]
    public class PersistentConfiguration
    {
        public const int _Version = 1;

        public string LogPath = "log.txt";

        public int Port = 1212;
        public string ServerName = "SS13 Server";
        public string serverMapName = "SavedMap";
        public string serverWelcomeMessage = "Welcome to the server!";
        public int serverMaxPlayers = 32;
        public GameType gameType = GameType.Game;
        public string AdminPassword = "admin123";
        public bool MessageLogging = false;
        public LogLevel LogLevel = LogLevel.Information;
        public float TickRate = 66;
    }
}
