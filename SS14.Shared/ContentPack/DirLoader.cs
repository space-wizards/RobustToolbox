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
            var fullPath = Path.GetFullPath(Path.Combine(_directory.FullName, relPath));

            if (!File.Exists(fullPath))
                return null;

            var bytes = File.ReadAllBytes(fullPath);
            return new MemoryStream(bytes, false);
        }
    }
}
