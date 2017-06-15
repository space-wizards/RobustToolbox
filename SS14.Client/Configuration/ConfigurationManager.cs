using SS14.Client.Interfaces.Configuration;
using SFML.Window;
using System;
using System.IO;
using System.Xml.Serialization;
using SS14.Shared.IoC;

namespace SS14.Client.Configuration
{
    [IoCTarget]
    public sealed class ConfigurationManager : IPlayerConfigurationManager
    {
        private string _configFile;
        public PlayerConfiguration Configuration { get; private set; }

        #region IConfigurationManager Members

        public void Initialize(string configFile)
        {
            if (File.Exists(configFile))
            {
                _configFile = configFile;
                var configLoader = new XmlSerializer(typeof(PlayerConfiguration));
                StreamReader configReader = File.OpenText(configFile);
                var config = (PlayerConfiguration)configLoader.Deserialize(configReader);
                configReader.Close();
                Configuration = config;

                config.ResourcePack = SS14.Shared.Utility.PlatformTools.SanePath(config.ResourcePack);
            }
            else
            {
                //if (LogManager.Singleton != null) LogManager.Singleton.LogMessage("ConfigManager: Could not load config. File not found. " + ConfigFileLoc);
            }
        }

        public void SetResolution(uint width, uint height)
        {
            Configuration.DisplayWidth = width;
            Configuration.DisplayHeight = height;
            Save();
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

        public Keyboard.Key GetConsoleKey()
        {
            return Configuration.ConsoleKey;
        }

        public bool GetVsync()
        {
            return Configuration.VSync;
        }

        public void SetVsync(bool state)
        {
            Configuration.VSync = state;
            Save();
        }

        public bool GetFullscreen()
        {
            return Configuration.Fullscreen;
        }

        public void SetFullscreen(bool fullscreen)
        {
            Configuration.Fullscreen = fullscreen;
            Save();
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

        public uint GetDisplayRefresh()
        {
            return Configuration.DisplayRefresh;
        }

        public void SetDisplayRefresh(uint rate)
        {
            Configuration.DisplayRefresh = rate;
            Save();
        }

        public uint GetDisplayHeight()
        {
            return Configuration.DisplayHeight;
        }

        public string GetServerAddress()
        {
            return Configuration.ServerAddress;
        }

        public bool GetMessageLogging()
        {
            return Configuration.MessageLogging;
        }

        public bool GetSimulateLatency()
        {
            return Configuration.SimulateLatency;
        }

        public float GetSimulatedLoss()
        {
            return Configuration.SimulatedLoss;
        }

        public float GetSimulatedMinimumLatency()
        {
            return Configuration.SimulatedMinimumLatency;
        }

        public float GetSimulatedRandomLatency()
        {
            return Configuration.SimulatedRandomLatency;
        }

        public int GetRate()
        {
            return Configuration.Rate;
        }

        public int GetUpdateRate()
        {
            return Configuration.UpdateRate;
        }

        public int GetCommandRate()
        {
            return Configuration.CommandRate;
        }

        public float GetInterpolation()
        {
            return Configuration.Interpolation;
        }

        #endregion IConfigurationManager Members

        private void Save()
        {
            if (Configuration == null)
            {
                //if (LogManager.Singleton != null) LogManager.Singleton.LogMessage("ConfigManager: Could not write config. No File loaded. " + Configuration.ToString() + " , " + ConfigFile);
            }
            else
            {
                var configSaver = new XmlSerializer(typeof(PlayerConfiguration));
                StreamWriter configWriter = File.CreateText(_configFile);
                configSaver.Serialize(configWriter, Configuration);
                configWriter.Flush();
                configWriter.Close();
            }
        }
    }
}
