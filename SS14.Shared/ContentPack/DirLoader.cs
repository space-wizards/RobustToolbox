using SS14.Shared.Log;
using System.Collections.Generic;
using System.IO;

namespace SS14.Shared.ContentPack
{
    /// <summary>
    ///     Holds info about a directory that is mounted in the VFS.
    /// </summary>
    internal class DirLoader : IContentRoot
    {
        private readonly DirectoryInfo _directory;

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="directory">Directory to mount.</param>
        public DirLoader(DirectoryInfo directory)
        {
            _directory = directory;
        }

        /// <inheritdoc />
        public bool Mount()
        {
            // Exists returns false if it actually exists, but no perms to read it
            return _directory.Exists;
        }

        /// <inheritdoc />
        public MemoryStream GetFile(string relPath)
        {
            var path = GetPath(relPath);
            if (path == null)
            {
                return null;
            }

            var bytes = File.ReadAllBytes(path);
            return new MemoryStream(bytes, false);
        }

        public string GetPath(string relPath)
        {
            var fullPath = Path.GetFullPath(Path.Combine(_directory.FullName, relPath));

            if (!File.Exists(fullPath))
                return null;

            return fullPath;
        }

        /// <inheritdoc />
        public IEnumerable<string> FindFiles(string path)
        {
            var fullPath = Path.GetFullPath(Path.Combine(_directory.FullName, path));
            var paths = PathHelpers.GetFiles(fullPath);

            // GetFiles returns full paths, we want them relative to root
            foreach (var filePath in paths)
            {
                yield return filePath.Substring(_directory.FullName.Length);
            }
        }
    }
}
