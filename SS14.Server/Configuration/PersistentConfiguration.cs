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

        public Vector2i ConsoleSize
        {
            get { return consoleSize; }
            set { consoleSize = value; }
        }
        private Vector2i consoleSize = new Vector2i(120, 60);

        public LogLevel LogLevel
        {
            get { return logLevel; }
            set { logLevel = value; }
        }
        private LogLevel logLevel = LogLevel.Information;

        public string LogPath
        {
            get { return logPath; }
            set { logPath = value; }
        }
        private string logPath = "logs";

        public string LogFormat
        {
            get { return logFormat; }
            set { logFormat = value; }
        }
        private string logFormat = "log_%(date)s-%(time)s.txt";

        public bool MessageLogging
        {
            get { return messageLogging; }
            set { messageLogging = value; }
        }
        private bool messageLogging = false;

        public int Port
        {
            get { return port; }
            set { port = value; }
        }
        private int port = 1212;

        public string ServerName
        {
            get { return serverName; }
            set { serverName = value; }
        }
        private string serverName = "SS13 Server";

        public float TickRate
        {
            get { return tickRate; }
            set { tickRate = value; }
        }
        private float tickRate = 66;

        public GameType gameType
        {
            get { return _gameType; }
            set { _gameType = value; }
        }
        private GameType _gameType = GameType.Game;

        public string serverMapName
        {
            get { return _serverMapName; }
            set { _serverMapName = value; }
        }
        private string _serverMapName = "SavedMap";

        public int serverMaxPlayers
        {
            get { return _serverMaxPlayers; }
            set { _serverMaxPlayers = value; }
        }
        private int _serverMaxPlayers = 32;

        public string serverWelcomeMessage
        {
            get { return _serverWelcomeMessage; }
            set { _serverWelcomeMessage = value; }
        }
        private string _serverWelcomeMessage = "Welcome to the server!";
    }
}
