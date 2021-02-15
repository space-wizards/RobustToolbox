using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Shared.ContentPack
{
    internal partial class ResourceManager
    {
        /// <summary>
        ///     Holds info about a directory that is mounted in the VFS.
        /// </summary>
        class DirLoader : IContentRoot
        {
            private readonly DirectoryInfo _directory;
            private readonly ISawmill _sawmill;

            /// <summary>
            ///     Constructor.
            /// </summary>
            /// <param name="directory">Directory to mount.</param>
            /// <param name="sawmill"></param>
            public DirLoader(DirectoryInfo directory, ISawmill sawmill)
            {
                _directory = directory;
                _sawmill = sawmill;
            }

            /// <inheritdoc />
            public void Mount()
            {
                // Looks good to me
                // Nothing to check here since the ResourceManager handles checking permissions.
            }

            /// <inheritdoc />
            public bool TryGetFile(ResourcePath relPath, [NotNullWhen(true)] out Stream? stream)
            {
                var path = GetPath(relPath);
                if (!File.Exists(path))
                {
                    stream = null;
                    return false;
                }

                CheckPathCasing(relPath);

                stream = File.OpenRead(path);
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
                    var relPath = filePath.Substring(_directory.FullName.Length);
                    yield return ResourcePath.FromRelativeSystemPath(relPath);
                }
            }

            [Conditional("DEBUG")]
            private void CheckPathCasing(ResourcePath path)
            {
                // Run this inside the thread pool due to overhead.
                Task.Run(() =>
                {
                    var prevPath = GetPath(ResourcePath.Root);
                    var diskPath = ResourcePath.Root;
                    var mismatch = false;
                    foreach (var segment in path.EnumerateSegments())
                    {
                        var prevDir = new DirectoryInfo(prevPath);
                        var found = false;
                        foreach (var info in prevDir.EnumerateFileSystemInfos())
                        {
                            if (!string.Equals(info.Name, segment, StringComparison.InvariantCultureIgnoreCase))
                            {
                                // Not the dir info for this path segment, ignore it.
                                continue;
                            }

                            if (!string.Equals(info.Name, segment, StringComparison.InvariantCulture))
                            {
                                // Segments match insensitively but not the other way around. Bad.
                                mismatch = true;
                            }

                            diskPath /= info.Name;
                            prevPath = Path.Combine(prevPath, info.Name);
                            found = true;
                            break;
                        }

                        if (!found)
                        {
                            // File doesn't exist. Let somebody else throw the exception.
                            return;
                        }
                    }

                    if (mismatch)
                    {
                        _sawmill.Warning("Path '{0}' has mismatching case from file on disk ('{1}'). " +
                                        "This can cause loading failures on certain file system configurations " +
                                        "and should be corrected.", path, diskPath);
                    }
                });
            }
            public IEnumerable<string> GetRelativeFilePaths()
            {
                return GetRelativeFilePaths(_directory);
            }

            private IEnumerable<string> GetRelativeFilePaths(DirectoryInfo dir)
            {
                foreach (var file in dir.EnumerateFiles())
                {
                    if ((file.Attributes & FileAttributes.Hidden) != 0 || file.Name[0] == '.')
                    {
                        continue;
                    }

                    var filePath = file.FullName;
                    var relPath = filePath.Substring(_directory.FullName.Length);
                    yield return ResourcePath.FromRelativeSystemPath(relPath).ToRootedPath().ToString();
                }

                foreach (var subDir in dir.EnumerateDirectories())
                {
                    if (((subDir.Attributes & FileAttributes.Hidden) != 0) || subDir.Name[0] == '.')
                    {
                        continue;
                    }

                    foreach (var relPath in GetRelativeFilePaths(subDir))
                    {
                        yield return relPath;
                    }
                }
            }
        }
    }
}
