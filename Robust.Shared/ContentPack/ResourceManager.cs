using System;
using System.Collections.Generic;
using System.IO;
using Robust.Shared.Configuration;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Shared.ContentPack
{
    /// <summary>
    ///     Virtual file system for all disk resources.
    /// </summary>
    public partial class ResourceManager : IResourceManagerInternal
    {
        private const string DataFolderName = "Space Station 14";

        [Dependency]
#pragma warning disable 649
        private readonly IConfigurationManager _config;
#pragma warning restore 649

        private readonly List<(ResourcePath prefix, IContentRoot root)> _contentRoots = new List<(ResourcePath, IContentRoot)>();

        /// <inheritdoc />
        public IWritableDirProvider UserData { get; private set; }

        /// <inheritdoc />
        public void Initialize(string userData)
        {
            if (userData != null)
            {
                UserData = new WritableDirProvider(Directory.CreateDirectory(userData));
            }
            else
            {
                UserData = new VirtualWritableDirProvider();
            }
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
        public void MountContentPack(string pack, ResourcePath prefix = null)
        {
            if (prefix == null)
            {
                prefix = ResourcePath.Root;
            }
            if (!prefix.IsRooted)
            {
                throw new ArgumentException("Prefix must be rooted.", nameof(prefix));
            }
            pack = PathHelpers.ExecutableRelativeFile(pack);

            var packInfo = new FileInfo(pack);

            if (!packInfo.Exists)
            {
                throw new FileNotFoundException("Specified ContentPack does not exist: " + packInfo.FullName);
            }

            //create new PackLoader
            var loader = new PackLoader(packInfo);
            loader.Mount();
            _contentRoots.Add((prefix, loader));
        }

        /// <inheritdoc />
        public void MountContentDirectory(string path, ResourcePath prefix = null)
        {
            if (prefix == null)
            {
                prefix = ResourcePath.Root;
            }
            if (!prefix.IsRooted)
            {
                throw new ArgumentException("Prefix must be rooted.", nameof(prefix));
            }
            path = PathHelpers.ExecutableRelativeFile(path);
            var pathInfo = new DirectoryInfo(path);
            if (!pathInfo.Exists)
            {
                throw new DirectoryNotFoundException("Specified directory does not exist: " + pathInfo.FullName);
            }

            var loader = new DirLoader(pathInfo);
            loader.Mount();
            _contentRoots.Add((prefix, loader));
        }

        /// <inheritdoc />
        public MemoryStream ContentFileRead(string path)
        {
            return ContentFileRead(new ResourcePath(path));
        }

        /// <inheritdoc />
        public MemoryStream ContentFileRead(ResourcePath path)
        {
            if (TryContentFileRead(path, out var fileStream))
            {
                return fileStream;
            }
            throw new FileNotFoundException($"Path does not exist in the VFS: '{path}'");
        }

        /// <inheritdoc />
        public bool TryContentFileRead(string path, out MemoryStream fileStream)
        {
            return TryContentFileRead(new ResourcePath(path), out fileStream);
        }

        /// <inheritdoc />
        public bool TryContentFileRead(ResourcePath path, out MemoryStream fileStream)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (!path.IsRooted)
            {
                throw new ArgumentException("Path must be rooted", nameof(path));
            }
            foreach ((var prefix, var root) in _contentRoots)
            {
                if (!path.TryRelativeTo(prefix, out var relative))
                {
                    continue;
                }
                if (root.TryGetFile(relative, out fileStream))
                {
                    return true;
                }
            }
            fileStream = null;
            return false;
        }

        /// <inheritdoc />
        public bool ContentFileExists(string path)
        {
            return ContentFileExists(new ResourcePath(path));
        }

        /// <inheritdoc />
        public bool ContentFileExists(ResourcePath path)
        {
            return TryContentFileRead(path, out var _);
        }

        /// <inheritdoc />
        public IEnumerable<ResourcePath> ContentFindFiles(string path)
        {
            return ContentFindFiles(new ResourcePath(path));
        }

        /// <inheritdoc />
        public IEnumerable<ResourcePath> ContentFindFiles(ResourcePath path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (!path.IsRooted)
            {
                throw new ArgumentException("Path is not rooted", nameof(path));
            }
            var alreadyReturnedFiles = new HashSet<ResourcePath>();
            foreach ((var prefix, var root) in _contentRoots)
            {
                if (!path.TryRelativeTo(prefix, out var relative))
                {
                    continue;
                }

                foreach (var filename in root.FindFiles(relative))
                {
                    var newpath = prefix / filename;
                    if (!alreadyReturnedFiles.Contains(newpath))
                    {
                        alreadyReturnedFiles.Add(newpath);
                        yield return newpath;
                    }
                }
            }
        }

        // TODO: Remove this when/if we can get Godot to load from not-the-filesystem.
        public bool TryGetDiskFilePath(ResourcePath path, out string diskPath)
        {
            // loop over each root trying to get the file
            foreach ((var prefix, var root) in _contentRoots)
            {
                if (!(root is DirLoader dirLoader) || !path.TryRelativeTo(prefix, out var tempPath))
                {
                    continue;
                }
                diskPath = dirLoader.GetPath(tempPath);
                if (File.Exists(diskPath))
                    return true;
            }
            diskPath = null;
            return false;
        }

        public void MountStreamAt(MemoryStream stream, ResourcePath path)
        {
            if (!path.IsRooted)
            {
                throw new ArgumentException("Path must be rooted.", nameof(path));
            }
            var loader = new SingleStreamLoader(stream, path.ToRelativePath());
            loader.Mount();
            _contentRoots.Add((ResourcePath.Root, loader));
        }
    }
}
