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
        void MountDefaultPack();

        /// <summary>
        /// Loads a content pack from disk into the VFS.
        /// </summary>
        /// <param name="pack"></param>
        /// <param name="password"></param>
        void MountContentPack(string pack, string password = null);

        void MountDirectory(string path);
        MemoryStream FileRead(string path);
        bool FileExists(string path);
        bool TryFileRead(string path, out MemoryStream fileStream);
    }
}