using System.IO;

namespace SS14.Shared.ContentPack
{
    internal class DirLoader : IContentRoot
    {
        private readonly DirectoryInfo _directory;

        public DirLoader(DirectoryInfo directory)
        {
            _directory = directory;
        }

        public bool Mount()
        {
            // Exists returns false if it actually exists, but no perms to read it
            return _directory.Exists;
        }

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
