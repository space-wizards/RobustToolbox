using System;
using System.Collections.Generic;
using System.IO;
using SS14.Shared.Configuration;
using SS14.Shared.GameLoader;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.IoC;
using SS14.Shared.Log;

namespace SS14.Shared.ContentPack
{
    /// <summary>
    /// Virtual file system for all disk resources.
    /// </summary>
    public class ResourceManager
    {
        private static ResourceManager _instance;
        public static ResourceManager Instance => _instance ?? (_instance = new ResourceManager());

        private List<IContentRoot> _contentRoots = new List<IContentRoot>();

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ResourceManager()
        {
            _config = IoCManager.Resolve<IConfigurationManager>();
        }

        /// <summary>
        /// Configuration Manager.
        /// </summary>
        [Dependency]
        private readonly IConfigurationManager _config;

        /// <summary>
        /// Sets the manager up so that the base game can run.
        /// </summary>
        public void Initialize()
        {
            _config.RegisterCVar("resource.pack", Path.Combine("..", "..", "Resources", "ResourcePack.zip"), CVarFlags.ARCHIVE);
            _config.RegisterCVar("resource.password", string.Empty, CVarFlags.SERVER | CVarFlags.REPLICATED);

            MountEngineContent();
        }

        /// <summary>
        /// Loads the content needed for the engine to run.
        /// </summary>
        private void MountEngineContent()
        {
            
        }

        /// <summary>
        /// Loads the default content pack from the configuration file into the VFS.
        /// </summary>
        public void MountDefaultPack()
        {
            //Assert server only

            string zipPath = _config.GetCVar<string>("resource.pack");
            string password = _config.GetCVar<string>("resource.password");

            // no pack in config
            if (string.IsNullOrWhiteSpace(zipPath))
            {
                Logger.Log("[RES] No default ContentPack to load in configuration.");
                return;
            }
            
            MountContentPack(zipPath, password);
        }

        /// <summary>
        /// Loads a content pack from disk into the VFS.
        /// </summary>
        /// <param name="pack"></param>
        /// <param name="password"></param>
        public void MountContentPack(string pack, string password = null)
        {
            if (AppDomain.CurrentDomain.GetAssemblyByName("SS14.UnitTesting") != null)
            {
                string debugPath = "..";
                pack = Path.Combine(debugPath, pack);
            }

            pack = PathHelpers.ExecutableRelativeFile(pack);

            var packInfo = new FileInfo(pack);

            if (!packInfo.Exists)
                throw new FileNotFoundException("Specified ContentPack does not exist: " + packInfo.FullName);

            //create new PackLoader
            var loader = new PackLoader(packInfo, password);

            if(loader.LoadPack())
            {
                //add packloader to sources
                _contentRoots.Add(loader);
            }
        }

        public void MountDirectory(string path)
        {
            
        }

        public MemoryStream GetFile(string path)
        {
            // loop over each root trying to get the file
            foreach (IContentRoot root in _contentRoots)
            {
                var file = root.GetFile(path);
                if (file != null)
                    return file;
            }
            return null;
        }
    }
}
