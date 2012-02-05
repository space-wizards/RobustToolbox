using System.IO;
using System.Xml.Serialization;
using ClientInterfaces;
using SS13_Shared;

namespace ClientServices.Configuration
{
    public sealed class ConfigurationManager : IService
    {
        public Configuration Configuration { get; private set; }
        private string _configFile;
        private IServiceManager _serviceManager;

        public ConfigurationManager(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

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

        public void Save()
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
    }
}
