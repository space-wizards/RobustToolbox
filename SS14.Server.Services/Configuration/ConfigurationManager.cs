using SFML.System;
using SS14.Server.Interfaces;
using SS14.Server.Interfaces.Configuration;
using SS14.Server.Services.Log;
using SS14.Shared;
using SS14.Shared.ServerEnums;
using System.Drawing;
using System.IO;
using System.Xml.Serialization;

namespace SS14.Server.Services.Configuration
{
    public sealed class ConfigurationManager : IServerConfigurationManager, IService
    {
        private string ConfigFile;
        public PersistentConfiguration Configuration;

        #region IConfigurationManager Members

        public void Initialize(string ConfigFileLoc)
        {
            if (File.Exists(ConfigFileLoc))
            {
                var ConfigLoader = new XmlSerializer(typeof (PersistentConfiguration));
                StreamReader ConfigReader = File.OpenText(ConfigFileLoc);
                var Config = (PersistentConfiguration) ConfigLoader.Deserialize(ConfigReader);
                ConfigReader.Close();
                Configuration = Config;
                ConfigFile = ConfigFileLoc;
            }
            else
            {
                //if (LogManager.Singleton != null) LogManager.Singleton.LogMessage("ConfigurationManager: Could not load config. File not found. " + ConfigFileLoc);
            }
        }

        public void Save()
        {
            if (Configuration == null)
            {
                //if (LogManager.Singleton != null) LogManager.Singleton.LogMessage("ConfigurationManager: Could not write config. No File loaded. " + Configuration.ToString() + " , " + ConfigFile);
                return;
            }
            else
            {
                var ConfigSaver = new XmlSerializer(Configuration.GetType());
                StreamWriter ConfigWriter = File.CreateText(ConfigFile);
                ConfigSaver.Serialize(ConfigWriter, Configuration);
                ConfigWriter.Flush();
                ConfigWriter.Close();
                LogManager.Log("Server configuration saved to '" + ConfigFile + "'.");
            }
        }

        public string ServerName
        {
            get { return Configuration.ServerName; }
            set { Configuration.ServerName = value; }
        }

        public string ServerMapName
        {
            get { return Configuration.serverMapName; }
            set { Configuration.serverMapName = value; }
        }

        public string ServerWelcomeMessage
        {
            get { return Configuration.serverWelcomeMessage; }
            set { Configuration.serverWelcomeMessage = value; }
        }

        public string AdminPassword
        {
            get { return Configuration.AdminPassword; }
            set { Configuration.AdminPassword = value; }
        }

        public string LogPath
        {
            get { return Configuration.LogPath; }
            set { Configuration.LogPath = value; }
        }

        public int Version
        {
            get { return PersistentConfiguration._Version; }
        }

        public int Port
        {
            get { return Configuration.Port; }
            set { Configuration.Port = value; }
        }

        public int ServerMaxPlayers
        {
            get { return Configuration.serverMaxPlayers; }
            set { Configuration.serverMaxPlayers = value; }
        }

        public GameType GameType
        {
            get { return Configuration.gameType; }
            set { Configuration.gameType = value; }
        }

        public bool MessageLogging
        {
            get { return Configuration.MessageLogging; }
            set { Configuration.MessageLogging = value; }
        }

        public LogLevel LogLevel
        {
            get { return Configuration.LogLevel; }
            set { Configuration.LogLevel = value; }
        }

        public float TickRate
        {
            get { return Configuration.TickRate; }
            set { Configuration.TickRate = value; }
        }

        public Vector2i ConsoleSize
        {
            get { return Configuration.ConsoleSize; }
            set { Configuration.ConsoleSize = value; }
        }

        #endregion

        #region IService Members

        public ServerServiceType ServiceType
        {
            get { return ServerServiceType.ConfigManager; }
        }

        #endregion

        public void LoadResources()
        {
        }
    }
}