using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Robust.Shared.ContentPack
{
    /// <summary>
    /// Provides an API for file and directory manipulation inside of a rooted folder.
    /// </summary>
    [PublicAPI]
    public interface IWritableDirProvider
    {
        /// <summary>
        /// The root path of this provider.
        /// Can be null if it's a virtual provider.
        /// </summary>
        string? RootDir { get; }

        /// <summary>
        /// Creates a directory. If the directory exists, does nothing.
        /// </summary>
        /// <param name="path">Path of directory to create.</param>
        void CreateDir(ResourcePath path);

        /// <summary>
        /// Deletes a file or empty directory. If the file or directory
        /// does not exist, does nothing.
        /// </summary>
        /// <param name="path">Path of object to delete.</param>
        void Delete(ResourcePath path);

        /// <summary>
        /// Tests if a file or directory exists.
        /// </summary>
        /// <param name="path">Path to test.</param>
        /// <returns>If the object exists.</returns>
        bool Exists(ResourcePath path);

        /// <summary>
        /// Finds all files and directories that match the expression. This will include empty directories.
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="recursive"></param>
        /// <returns>A tuple that contains collections of files, directories that matched the expression.</returns>
        (IEnumerable<ResourcePath> files, IEnumerable<ResourcePath> directories) Find(string pattern,
            bool recursive = true);

        /// <summary>
        /// Tests if a path is a directory.
        /// </summary>
        /// <param name="path">Path to test.</param>
        /// <returns>True if it is a directory, false if it is a file.</returns>
        bool IsDir(ResourcePath path);

        /// <summary>
        /// Attempts to open a file.
        /// </summary>
        /// <param name="path">Path of file to open.</param>
        /// <param name="fileMode">Options on how to open the file.</param>
        /// <param name="access">Specifies the operations that can be performed on the file.</param>
        /// <param name="share">Specifies the access other threads have to the file.</param>
        /// <returns>A valid file stream.</returns>
        /// <exception cref="FileNotFoundException">
        ///     Thrown if the file does not exist.
        /// </exception>
        Stream Open(ResourcePath path, FileMode fileMode, FileAccess access, FileShare share);

        /// <summary>
        /// Attempts to open a file.
        /// </summary>
        /// <param name="path">Path of file to open.</param>
        /// <param name="fileMode">Options on how to open the file.</param>
        /// <returns>A valid file stream.</returns>
        /// <exception cref="FileNotFoundException">
        ///     Thrown if the file does not exist.
        /// </exception>
        Stream Open(ResourcePath path, FileMode fileMode)
        {
            return Open(path,
                fileMode,
                fileMode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite,
                FileShare.None);
        }

        /// <summary>
        /// Attempts to rename a file.
        /// </summary>
        /// <param name="oldPath">Path of the file to rename.</param>
        /// <param name="newPath">New name of the file.</param>
        /// <returns></returns>
        void Rename(ResourcePath oldPath, ResourcePath newPath);
    }
}