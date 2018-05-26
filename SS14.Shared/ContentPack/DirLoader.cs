using SS14.Shared.Log;
using SS14.Shared.Utility;
using System.Collections.Generic;
using System.IO;

namespace SS14.Shared.ContentPack
{
    public partial class ResourceManager
    {
        /// <summary>
        ///     Holds info about a directory that is mounted in the VFS.
        /// </summary>
        class DirLoader : IContentRoot
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
            public void Mount()
            {
                // Looks good to me
                // Nothing to check here since the ResourceManager handles checking permissions.
            }

            /// <inheritdoc />
            public bool TryGetFile(ResourcePath relPath, out MemoryStream stream)
            {
                var path = GetPath(relPath);
                if (!File.Exists(path))
                {
                    stream = null;
                    return false;
                }

                var bytes = File.ReadAllBytes(path);
                stream = new MemoryStream(bytes, false);
                return true;
            }

            internal string GetPath(ResourcePath relPath)
            {
                return Path.GetFullPath(Path.Combine(_directory.FullName, relPath.ToRelativeSystemPath()));
            }

            /// <inheritdoc />
            public IEnumerable<ResourcePath> FindFiles(ResourcePath path)
            {
                var fullPath = GetPath(path);
                if (!Directory.Exists(fullPath))
                {
                    yield break;
                }
                var paths = PathHelpers.GetFiles(fullPath);

                // GetFiles returns full paths, we want them relative to root
                foreach (var filePath in paths)
                {
                    var relpath = filePath.Substring(_directory.FullName.Length);
                    yield return ResourcePath.FromRelativeSystemPath(relpath);
                }
            }
        }
    }
}
