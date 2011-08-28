using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace SS3D_Server.Modules
{
    public sealed class ConfigManager
    {
        public Configuration Configuration;
        private string ConfigFile;

        static readonly ConfigManager singleton = new ConfigManager();

        static ConfigManager()
        {
        }

        ConfigManager()
        {
        }

        public static ConfigManager Singleton
        {
            get
            {
                return singleton;
            }
        }

        public void Initialize(string ConfigFileLoc)
        {
            if (File.Exists(ConfigFileLoc))
            {
                System.Xml.Serialization.XmlSerializer ConfigLoader = new System.Xml.Serialization.XmlSerializer(typeof(Configuration));
                StreamReader ConfigReader = File.OpenText(ConfigFileLoc);
                Configuration Config = (Configuration)ConfigLoader.Deserialize(ConfigReader);
                ConfigReader.Close();
                Configuration = Config;
                ConfigFile = ConfigFileLoc;
            }
            else
            {
                //if (LogManager.Singleton != null) LogManager.Singleton.LogMessage("ConfigManager: Could not load config. File not found. " + ConfigFileLoc);
            }
        }

        public void Save()
        {
            if (Configuration == null)
            {
                //if (LogManager.Singleton != null) LogManager.Singleton.LogMessage("ConfigManager: Could not write config. No File loaded. " + Configuration.ToString() + " , " + ConfigFile);
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
    }

    [Serializable]
    public class Configuration
    {
        const int _Version = 1;

        public int Port = 1212;
        public string ServerName = "SS3D Server"; 
        public string serverMapName = "SavedMap";
        public string serverWelcomeMessage = "Welcome to the server!";
        public int serverMaxPlayers = 32;
        public GameType gameType = GameType.Game;
        public int framePeriod = 33; // The time (in milliseconds) between server frames
    }
}
