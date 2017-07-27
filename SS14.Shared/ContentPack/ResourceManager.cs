using System;
using System.Collections.Generic;
using System.IO;
using SS14.Shared.Configuration;
using SS14.Shared.GameLoader;
using SS14.Shared.Interfaces;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.IoC;
using SS14.Shared.Log;

namespace SS14.Shared.ContentPack
{
    /// <summary>
    ///     Virtual file system for all disk resources.
    /// </summary>
    public class ResourceManager : IResourceManager
    {
        /// <summary>
        ///     Configuration Manager.
        /// </summary>
        [Dependency] private readonly IConfigurationManager _config;

        private readonly List<IContentRoot> _contentRoots = new List<IContentRoot>();

        /// <summary>
        ///     Default constructor.
        /// </summary>
        public ResourceManager()
        {
            _config = IoCManager.Resolve<IConfigurationManager>();
        }

        /// <summary>
        ///     Sets the manager up so that the base game can run.
        /// </summary>
        public void Initialize()
        {
            _config.RegisterCVar("resource.pack", Path.Combine("..", "..", "Resources", "ResourcePack.zip"),
                CVarFlags.ARCHIVE);
            _config.RegisterCVar("resource.password", string.Empty, CVarFlags.SERVER | CVarFlags.REPLICATED);
        }

        /// <summary>
        ///     Loads the default content pack from the configuration file into the VFS.
        /// </summary>
        public void MountDefaultPack()
        {
            //Assert server only

            var zipPath = _config.GetCVar<string>("resource.pack");
            var password = _config.GetCVar<string>("resource.password");

            // no pack in config
            if (string.IsNullOrWhiteSpace(zipPath))
            {
                Logger.Log("[RES] No default ContentPack to load in configuration.");
                return;
            }

            MountContentPack(zipPath, password);
        }

        /// <summary>
        ///     Loads a content pack from disk into the VFS.
        /// </summary>
        /// <param name="pack"></param>
        /// <param name="password"></param>
        public void MountContentPack(string pack, string password = null)
        {
            if (AppDomain.CurrentDomain.GetAssemblyByName("SS14.UnitTesting") != null)
            {
                var debugPath = "..";
                pack = Path.Combine(debugPath, pack);
            }

            pack = PathHelpers.ExecutableRelativeFile(pack);

            var packInfo = new FileInfo(pack);

            if (!packInfo.Exists)
                throw new FileNotFoundException("Specified ContentPack does not exist: " + packInfo.FullName);

            //create new PackLoader
            var loader = new PackLoader(packInfo, password);

            if (loader.LoadPack())
                _contentRoots.Add(loader);
        }

        public void MountDirectory(string path)
        {
            throw new NotImplementedException();
        }

        public MemoryStream FileRead(string path)
        {
            // loop over each root trying to get the file
            foreach (var root in _contentRoots)
            {
                var file = root.GetFile(path);
                if (file != null)
                    return file;
            }
            return null;
        }

        public bool TryFileRead(string path, out MemoryStream fileStream)
        {
            var file = FileRead(path);
            if (file != null)
            {
                fileStream = file;
                return true;
            }
            fileStream = default(MemoryStream);
            return false;
        }

        public bool FileExists(string path)
        {
            throw new NotImplementedException();
        }
    }
}
