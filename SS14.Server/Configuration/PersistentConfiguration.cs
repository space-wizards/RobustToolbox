using SFML.System;
using SS14.Shared;
using SS14.Shared.Log;
using SS14.Shared.Utility;
using System;

namespace SS14.Server.Configuration
{
    [Serializable]
    public class PersistentConfiguration
    {
        public const int _Version = 1;
        public Vector2i ConsoleSize = new Vector2i(120, 60);
        public LogLevel LogLevel = LogLevel.Information;

        public string LogPath = "logs";
        public string LogFormat = "log_%(date)s-%(time)s.txt";
        public bool MessageLogging = false;

        public int Port = 1212;
        public string ServerName = "SS13 Server";
        public float TickRate = 66;
        public GameType gameType = GameType.Game;
        public string serverMapName = "SavedMap";
        public int serverMaxPlayers = 32;
        public string serverWelcomeMessage = "Welcome to the server!";
    }
}
