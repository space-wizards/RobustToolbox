using SS14.Shared.Utility;
using System.Collections.Generic;
using System.IO;

namespace SS14.Shared.ContentPack
{
    public partial class ResourceManager
    {
        /// <summary>
        ///     Common interface for mounting various things in the VFS.
        /// </summary>
        protected interface IContentRoot
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
            /// <returns>A stream of the file loaded into memory.</returns>
            bool TryGetFile(ResourcePath relPath, out MemoryStream stream);

            /// <summary>
            ///     Recursively finds all files in a directory and all sub directories.
            /// </summary>
            /// <param name="path">Directory to search inside of.</param>
            /// <returns>Enumeration of all relative file paths of the files found.</returns>
            IEnumerable<ResourcePath> FindFiles(ResourcePath path);
        }
    }
}
