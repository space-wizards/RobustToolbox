using System;
using System.Collections.Generic;
using System.IO;
using SS14.Shared.Configuration;
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
        [Dependency] private readonly IConfigurationManager _config;

        private readonly List<IContentRoot> _contentRoots = new List<IContentRoot>();

        /// <summary>
        ///     Default constructor.
        /// </summary>
        public ResourceManager()
        {
            _config = IoCManager.Resolve<IConfigurationManager>();
        }

        /// <inheritdoc />
        public void Initialize()
        {
            _config.RegisterCVar("resource.pack", Path.Combine("..", "..", "Resources", "ResourcePack.zip"),
                CVarFlags.ARCHIVE);
            _config.RegisterCVar("resource.password", string.Empty, CVarFlags.SERVER | CVarFlags.REPLICATED);
        }

        /// <inheritdoc />
        public void MountDefaultContentPack()
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

        /// <inheritdoc />
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

            if (loader.Mount())
                _contentRoots.Add(loader);
        }

        /// <inheritdoc />
        public void MountContentDirectory(string path)
        {
            path = PathHelpers.ExecutableRelativeFile(path);
            var pathInfo = new DirectoryInfo(path);

            var loader = new DirLoader(pathInfo);
            if (loader.Mount())
                _contentRoots.Add(loader);
        }

        /// <inheritdoc />
        public MemoryStream ContentFileRead(string path)
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

        /// <inheritdoc />
        public bool TryContentFileRead(string path, out MemoryStream fileStream)
        {
            var file = ContentFileRead(path);
            if (file != null)
            {
                fileStream = file;
                return true;
            }
            fileStream = default(MemoryStream);
            return false;
        }

        /// <inheritdoc />
        public bool ContentFileExists(string path)
        {
            throw new NotImplementedException();
        }
    }
}
