using System.IO;
using System.Xml.Serialization;
using ClientInterfaces;

namespace ClientServices.Configuration
{
    public sealed class ConfigurationManager : IConfigurationManager
    {
        public Configuration Configuration { get; private set; }
        private string _configFile;

        public void Initialize(string configFile)
        {
            if (File.Exists(configFile))
            {
                _configFile = configFile;
                var configLoader = new XmlSerializer(typeof(Configuration));
                var configReader = File.OpenText(configFile);
                var config = (Configuration)configLoader.Deserialize(configReader);
                configReader.Close();
                Configuration = config;
            }
            else
            {
                //if (LogManager.Singleton != null) LogManager.Singleton.LogMessage("ConfigManager: Could not load config. File not found. " + ConfigFileLoc);
            }
        }

        private void Save()
        {
            if (Configuration == null)
            {
                //if (LogManager.Singleton != null) LogManager.Singleton.LogMessage("ConfigManager: Could not write config. No File loaded. " + Configuration.ToString() + " , " + ConfigFile);
            }
            else
            {
                var configSaver = new XmlSerializer(typeof(Configuration));
                var configWriter = File.CreateText(_configFile);
                configSaver.Serialize(configWriter, Configuration);
                configWriter.Flush();
                configWriter.Close();
            }
        }

        public void SetPlayerName(string name)
        {
            Configuration.PlayerName = name;
            Save();
        }

        public string GetPlayerName()
        {
            return Configuration.PlayerName;
        }

        public void SetServerAddress(string address)
        {
            Configuration.ServerAddress = address;
            Save();
        }

        public string GetResourcePath()
        {
            return Configuration.ResourcePack;
        }

        public string GetResourcePassword()
        {
            return Configuration.ResourcePassword;
        }

        public uint GetDisplayWidth()
        {
            return Configuration.DisplayWidth;
        }

        public uint GetDisplayHeight()
        {
            return Configuration.DisplayHeight;
        }
    }
}
