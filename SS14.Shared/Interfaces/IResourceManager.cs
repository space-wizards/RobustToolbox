using System.IO;

namespace SS14.Shared.Interfaces
{
    /// <summary>
    ///     Virtual file system for all disk resources.
    /// </summary>
    public interface IResourceManager
    {
        /// <summary>
        /// Sets the manager up so that the base game can run.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Loads the default content pack from the configuration file into the VFS.
        /// </summary>
        void MountDefaultContentPack();

        /// <summary>
        /// Loads a content pack from disk into the VFS.
        /// </summary>
        /// <param name="pack"></param>
        /// <param name="password"></param>
        void MountContentPack(string pack, string password = null);

        /// <summary>
        /// Adds a directory to search inside of to the VFS.
        /// </summary>
        /// <param name="path"></param>
        void MountContentDirectory(string path);
        
        /// <summary>
        /// Read a file from the mounted content roots.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        MemoryStream ContentFileRead(string path);

        /// <summary>
        /// Check if a file exists in any of the mounted content roots.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        bool ContentFileExists(string path);

        /// <summary>
        /// Try to read a file from the mounted content roots.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="fileStream"></param>
        /// <returns></returns>
        bool TryContentFileRead(string path, out MemoryStream fileStream);
    }
}