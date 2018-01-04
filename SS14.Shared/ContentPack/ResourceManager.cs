using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private const string DataFolderName = "Space Station 14";

        [Dependency]
        private readonly IConfigurationManager _config;

        private DirectoryInfo _configRoot;
        private readonly List<IContentRoot> _contentRoots = new List<IContentRoot>();

        /// <inheritdoc />
        public string ConfigDirectory => _configRoot.FullName;

        /// <inheritdoc />
        public void Initialize()
        {
            _config.RegisterCVar("resource.pack", "ResourcePack.zip", CVar.ARCHIVE);

            var configRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            configRoot = Path.Combine(configRoot, DataFolderName);
            configRoot = Path.GetFullPath(configRoot);
            _configRoot = Directory.CreateDirectory(configRoot);
        }

        /// <inheritdoc />
        public void MountDefaultContentPack()
        {
            //Assert server only

            var zipPath = _config.GetCVar<string>("resource.pack");

            // no pack in config
            if (string.IsNullOrWhiteSpace(zipPath))
            {
                Logger.Warning("[RES] No default ContentPack to load in configuration.");
                return;
            }

            MountContentPack(zipPath);
        }

        /// <inheritdoc />
        public void MountContentPack(string pack)
        {
            pack = PathHelpers.ExecutableRelativeFile(pack);

            var packInfo = new FileInfo(pack);

            if (!packInfo.Exists)
                throw new FileNotFoundException("Specified ContentPack does not exist: " + packInfo.FullName);

            //create new PackLoader
            var loader = new PackLoader(packInfo);

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
            {
                _contentRoots.Add(loader);
            }
            else
            {
                Logger.Error($"Unable to mount content directory: {path}");
            }
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

        /// <inheritdoc />
        public IEnumerable<string> ContentFindFiles(string path)
        {
            // some LINQ magic
            return _contentRoots
                .Select(root => root.FindFiles(path)) // get a collection of strings (paths) from each root
                .SelectMany(x => x) // merge the collections together to one big collection of strings
                .Distinct(); // remove duplicate strings
        }
    }
}
