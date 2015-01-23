using SS14.Shared;
using SS14.Shared.ServerEnums;
using System;
using System.Drawing;

namespace SS14.Server.Services.Configuration
{
    [Serializable]
    public class PersistentConfiguration
    {
        public const int _Version = 1;
        public string AdminPassword = "admin123";
        public Size ConsoleSize = new Size(120, 60);
        public LogLevel LogLevel = LogLevel.Information;

        public string LogPath = "log.txt";
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