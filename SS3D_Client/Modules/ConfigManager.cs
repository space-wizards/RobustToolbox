using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Xml;
using System.Xml.Serialization;

using Mogre;
using Miyagi;
using Miyagi.Common.Resources;

namespace SS3D.Modules
{
    public sealed class ConfigManager
    {
        public Configuration Configuration;
        private string ConfigFile;

        private const string ResourceGroupName = "Default";

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
                if (LogManager.Singleton != null) LogManager.Singleton.LogMessage("ConfigManager: Could not load config. File not found. " + ConfigFileLoc);
            }
        }

        public void Save()
        {
            if (Configuration == null)
            {
                if (LogManager.Singleton != null) LogManager.Singleton.LogMessage("ConfigManager: Could not write config. No File loaded. " + Configuration.ToString() + " , " + ConfigFile);
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
            if (!ResourceGroupManager.Singleton.ResourceGroupExists(ResourceGroupName)) ResourceGroupManager.Singleton.CreateResourceGroup(ResourceGroupName);

            if (Configuration == null || Configuration.Resources.Count == 0)
            {
                if (LogManager.Singleton != null) LogManager.Singleton.LogMessage("ConfigManager: Could not load ressources. No config loaded or no ressources specified. Config: " + Configuration.ToString() + ", Ressources: " + Configuration.Resources.Count.ToString());
                return;
            }
            else
            {
                #region General Resources
                foreach (string ResLoc in Configuration.Resources)
                {
                    if (Directory.Exists(ResLoc))
                    {
                        ResourceGroupManager.Singleton.AddResourceLocation(ResLoc, "FileSystem", ResourceGroupName);
                        continue;
                    }
                    else if (File.Exists(ResLoc))
                    {
                        ResourceGroupManager.Singleton.AddResourceLocation(ResLoc, "Zip", ResourceGroupName);
                        continue;
                    }
                }
                InitGroup(ResourceGroupName);
                LoadGroup(ResourceGroupName);
                #endregion

                #region Miyagi Skins
                var skins = new List<Skin>();
                foreach (string MSkinLoc in Configuration.MiyagiSkins)
                {
                    if (Directory.Exists(MSkinLoc))
                    {
                        string[] Files = Directory.GetFiles(MSkinLoc, "*.xml");
                        foreach (string fontFile in Files)
                        {
                            skins.AddRange(Skin.CreateFromXml(Path.Combine(MSkinLoc, fontFile), MiyagiResources.Singleton.mMiyagiSystem));
                        }
                        continue;
                    }
                    else if (File.Exists(MSkinLoc))
                    {
                        skins.AddRange(Skin.CreateFromXml(MSkinLoc, MiyagiResources.Singleton.mMiyagiSystem));
                        continue;
                    }
                }
                MiyagiResources.Singleton.Skins = skins.ToDictionary(s => s.Name); 
                #endregion

                #region Miyagi Fonts
                var fonts = new[]
                        {
                            TrueTypeFont.CreateFromXml(Configuration.MiyagiTrueTypeFonts, MiyagiResources.Singleton.mMiyagiSystem)
                                .Cast<Miyagi.Common.Resources.Font>().ToDictionary(f => f.Name),

                            ImageFont.CreateFromXml(Configuration.MiyagiImageFonts, MiyagiResources.Singleton.mMiyagiSystem)
                                .Cast<Miyagi.Common.Resources.Font>().ToDictionary(f => f.Name)
                        };

                MiyagiResources.Singleton.Fonts = fonts.SelectMany(dict => dict).ToDictionary(pair => pair.Key, pair => pair.Value);

                Miyagi.Common.Resources.Font.Default = MiyagiResources.Singleton.Fonts["BlueHighway"]; //This needs to be the default because we can't change dialog box fonts.
                #endregion
            }
        }

        public void InitGroup(string _groupName)
        {
            if (!ResourceGroupManager.Singleton.IsResourceGroupInitialised(_groupName))
                ResourceGroupManager.Singleton.InitialiseResourceGroup(_groupName);
        }

        public void LoadGroup(string _groupName)
        {
            if (!ResourceGroupManager.Singleton.IsResourceGroupLoaded(_groupName))
                ResourceGroupManager.Singleton.LoadResourceGroup(_groupName);
        }
    }

    [Serializable]
    public class Configuration
    {
        const int _Version = 1;

        public uint DisplayWidth = 1024;
        public uint DisplayHeight = 768;
        public bool Fullscreen = false;
        public bool VSync = true;
        public int FSAA = 0;
        public int TextureFiltering = (int)TextureFilterOptions.TFO_ANISOTROPIC;
        public int AnisotropicLevel = 8;
        public int NumMipmaps = 8;

        public List<string> Resources = new List<string>();
        public string MiyagiTrueTypeFonts = "";
        public string MiyagiImageFonts = "";
        public List<string> MiyagiSkins = new List<string>();

        public string PlayerName = "George Melons";
    }
}
