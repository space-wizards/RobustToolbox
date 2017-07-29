using System.IO;

namespace SS14.Shared.Interfaces
{
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
        

        MemoryStream ContentFileRead(string path);
        bool ContentFileExists(string path);
        bool TryContentFileRead(string path, out MemoryStream fileStream);
    }
}