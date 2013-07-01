using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Xml;
using System.Xml.Serialization;
using SS13_Shared;
using ServerInterfaces;
using ServerInterfaces.Configuration;
using SS13_Shared.ServerEnums;

namespace ServerServices.Configuration
{
    public sealed class ConfigurationManager: IConfigurationManager, IService
    {
        public PersistentConfiguration Configuration;
        private string ConfigFile;
        
        public void Initialize(string ConfigFileLoc)
        {
            if (File.Exists(ConfigFileLoc))
            {
                System.Xml.Serialization.XmlSerializer ConfigLoader = new System.Xml.Serialization.XmlSerializer(typeof(PersistentConfiguration));
                StreamReader ConfigReader = File.OpenText(ConfigFileLoc);
                PersistentConfiguration Config = (PersistentConfiguration)ConfigLoader.Deserialize(ConfigReader);
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
                System.Xml.Serialization.XmlSerializer ConfigSaver = new System.Xml.Serialization.XmlSerializer(Configuration.GetType());
                StreamWriter ConfigWriter = File.CreateText(ConfigFile);
                ConfigSaver.Serialize(ConfigWriter, Configuration);
                ConfigWriter.Flush();
                ConfigWriter.Close();
            }
        }

        public void LoadResources()
        {
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

        public int FramePeriod
        {
            get { return Configuration.framePeriod; }
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

        public ServerServiceType ServiceType
        {
            get { return ServerServiceType.ConfigManager; }
        }

        public int UpdatesPerSecond
        {
            get { return Configuration.UpdatesPerSecond; }
            set { Configuration.UpdatesPerSecond = value; }
        }
    }
}
