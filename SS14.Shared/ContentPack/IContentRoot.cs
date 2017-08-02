using System.IO;

namespace SS14.Shared.ContentPack
{
    /// <summary>
    ///     Common interface for mounting various things in the VFS
    /// </summary>
    internal interface IContentRoot
    {
        /// <summary>
        ///     Initializes the content root.
        /// </summary>
        /// <returns>If the content was mounted properly.</returns>
        bool Mount();

        /// <summary>
        ///     Gets a file from the content root using the relative path.
        /// </summary>
        /// <param name="relPath">Relative path from the root directory.</param>
        /// <returns>A stream of the file loaded into memory.</returns>
        MemoryStream GetFile(string relPath);
    }
}
