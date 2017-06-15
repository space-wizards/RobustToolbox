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

        public Vector2i ConsoleSize { get; set; } = new Vector2i(120, 60);
        public LogLevel LogLevel { get; set; } = LogLevel.Information;
        public string LogPath { get; set; } = "logs";
        public string LogFormat { get; set; } = "log_%(date)s-%(time)s.txt";
        public bool MessageLogging { get; set; } = false;
        public int Port { get; set; } = 1212;
        public string ServerName { get; set; } = "SS13 Server";
        public float TickRate { get; set; } = 66;
        public GameType gameType { get; set; } = GameType.Game;
        public string serverMapName { get; set; } = "SavedMap";
        public int serverMaxPlayers { get; set; } = 32;
        public string serverWelcomeMessage { get; set; } = "Welcome to the server!";
    }
}
