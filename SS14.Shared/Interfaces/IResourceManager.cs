using System.Collections.Generic;
using System.IO;

namespace SS14.Shared.Interfaces
{
    /// <summary>
    ///     Virtual file system for all disk resources.
    /// </summary>
    public interface IResourceManager
    {
        /// <summary>
        ///     Sets the manager up so that the base game can run.
        /// </summary>
        void Initialize();

        /// <summary>
        ///     Loads the default content pack from the configuration file into the VFS.
        /// </summary>
        void MountDefaultContentPack();

        /// <summary>
        ///     Loads a content pack from disk into the VFS. The path is relative to
        ///     the executable location on disk.
        /// </summary>
        /// <param name="pack"></param>
        void MountContentPack(string pack, string prefix=null);

        /// <summary>
        ///     Adds a directory to search inside of to the VFS. The directory is relative to
        ///     the executable location on disk.
        /// </summary>
        /// <param name="path"></param>
        void MountContentDirectory(string path, string prefix=null);

        /// <summary>
        ///     Read a file from the mounted content roots.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        MemoryStream ContentFileRead(string path);

        /// <summary>
        ///     Check if a file exists in any of the mounted content roots.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        bool ContentFileExists(string path);

        /// <summary>
        ///     Try to read a file from the mounted content roots.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="fileStream"></param>
        /// <returns></returns>
        bool TryContentFileRead(string path, out MemoryStream fileStream);

        /// <summary>
        ///     Recursively finds all files in a directory and all sub directories.
        /// </summary>
        /// <param name="path">Directory to search inside of.</param>
        /// <returns>Enumeration of all relative file paths of the files found.</returns>
        IEnumerable<string> ContentFindFiles(string path);

        /// <summary>
        ///     Absolute path to the configuration directory for the game. If you are writing any files,
        ///     they need to be inside of this directory.
        /// </summary>
        string ConfigDirectory { get; }
    }
}
