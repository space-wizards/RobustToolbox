using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Robust.Shared.Utility;

namespace Robust.Shared.ContentPack
{
    /// <summary>
    ///     Common interface for mounting various things in the VFS.
    /// </summary>
    public interface IContentRoot
    {
        /// <summary>
        ///     Initializes the content root.
        ///     Throws an exception if the content root failed to mount.
        /// </summary>
        void Mount();

        /// <summary>
        ///     Gets a file from the content root using the relative path.
        /// </summary>
        /// <param name="relPath">Relative path from the root directory.</param>
        /// <param name="stream"></param>
        /// <returns>A stream of the file loaded into memory.</returns>
        bool TryGetFile(ResPath relPath, [NotNullWhen(true)] out Stream? stream);

        /// <summary>
        ///     Returns true if the given file exists.
        /// </summary>
        public bool FileExists(ResPath relPath);

        /// <summary>
        ///     Recursively finds all files in a directory and all sub directories.
        /// </summary>
        /// <param name="path">Directory to search inside of.</param>
        /// <returns>Enumeration of all relative file paths of the files found.</returns>
        IEnumerable<ResPath> FindFiles(ResPath path);

        /// <summary>
        ///     Recursively returns relative paths to resource files.
        /// </summary>
        /// <returns>Enumeration of all relative file paths.</returns>
        IEnumerable<string> GetRelativeFilePaths();

        IEnumerable<string> GetEntries(ResPath path)
        {
            var countDirs = path == ResPath.Self ? 0 : path.CanonPath.Split('/').Count();

            var options = FindFiles(path).Select(c =>
            {
                var segment = c.CanonPath.Split('/');
                var segCount = segment.Count();
                var newPath = segment.Skip(countDirs).First();
                if (segCount > countDirs + 1)
                    newPath += "/";

                return newPath;
            }).Distinct();

            return options;
        }
    }
}
