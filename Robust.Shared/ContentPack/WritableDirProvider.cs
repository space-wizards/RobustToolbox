using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Shared.ContentPack
{
    /// <inheritdoc />
    internal sealed class WritableDirProvider : IWritableDirProvider
    {
        private readonly bool _hideRootDir;

        public string RootDir { get; }

        string? IWritableDirProvider.RootDir => _hideRootDir ? null : RootDir;

        /// <summary>
        /// Constructs an instance of <see cref="WritableDirProvider"/>.
        /// </summary>
        /// <param name="rootDir">Root file system directory to allow writing.</param>
        /// <param name="hideRootDir">If true, <see cref="IWritableDirProvider.RootDir"/> is reported as null.</param>
        public WritableDirProvider(DirectoryInfo rootDir, bool hideRootDir)
        {
            // FullName does not have a trailing separator, and we MUST have a separator.
            RootDir = rootDir.FullName + Path.DirectorySeparatorChar.ToString();
            _hideRootDir = hideRootDir;
        }

        #region File Access

        /// <inheritdoc />
        public void CreateDir(ResourcePath path)
        {
            var fullPath = GetFullPath(path);
            Directory.CreateDirectory(fullPath);
        }

        /// <inheritdoc />
        public void Delete(ResourcePath path)
        {
            var fullPath = GetFullPath(path);
            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, true);
            }
            else if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        /// <inheritdoc />
        public bool Exists(ResourcePath path)
        {
            var fullPath = GetFullPath(path);
            return Directory.Exists(fullPath) || File.Exists(fullPath);
        }

        /// <inheritdoc />
        public (IEnumerable<ResourcePath> files, IEnumerable<ResourcePath> directories) Find(string pattern, bool recursive = true)
        {
            var rootLen = RootDir.Length - 1;
            var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            var files = Directory.GetFiles(RootDir, pattern, option);
            var dirs = Directory.GetDirectories(RootDir, pattern, option);

            var resFiles = new List<ResourcePath>(files.Length);
            var resDirs = new List<ResourcePath>(dirs.Length);

            foreach (var file in files)
            {
                resFiles.Add(new ResourcePath(file.Substring(rootLen), "\\"));
            }

            foreach (var dir in dirs)
            {
                resDirs.Add(new ResourcePath(dir.Substring(rootLen), "\\"));
            }

            return (resFiles, resDirs);
        }

        public IEnumerable<string> DirectoryEntries(ResourcePath path)
        {
            var fullPath = GetFullPath(path);

            if (!Directory.Exists(fullPath))
                yield break;

            foreach (var entry in Directory.EnumerateFileSystemEntries(fullPath))
            {
                yield return Path.GetRelativePath(fullPath, entry);
            }
        }

        /// <inheritdoc />
        public bool IsDir(ResourcePath path)
        {
            return Directory.Exists(GetFullPath(path));
        }

        /// <inheritdoc />
        public Stream Open(ResourcePath path, FileMode fileMode, FileAccess access, FileShare share)
        {
            var fullPath = GetFullPath(path);
            return File.Open(fullPath, fileMode, access, share);
        }

        /// <inheritdoc />
        public void Rename(ResourcePath oldPath, ResourcePath newPath)
        {
            var fullOldPath = GetFullPath(oldPath);
            var fullNewPath = GetFullPath(newPath);
            File.Move(fullOldPath, fullNewPath);
        }

        #endregion

        public string GetFullPath(ResourcePath path)
        {
            if (!path.IsRooted)
            {
                throw new ArgumentException("Path must be rooted.");
            }

            return PathHelpers.SafeGetResourcePath(RootDir, path);
        }
    }
}
