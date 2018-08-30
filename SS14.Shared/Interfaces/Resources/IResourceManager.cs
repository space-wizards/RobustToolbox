using System;
using System.Collections.Generic;
using System.IO;
using SS14.Shared.Utility;

namespace SS14.Shared.Interfaces.Resources
{
    /// <summary>
    ///     Virtual file system for all disk resources.
    /// </summary>
    public interface IResourceManager
    {
        /// <summary>
        ///     Provides access to the writable user data folder.
        /// </summary>
        IWritableDirProvider UserData { get; }

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
        /// <param name="pack">The path of the pack to load on disk.</param>
        /// <param name="prefix">The resource path to which all files in the pack will be relative to in the VFS.</param>
        /// <exception cref="FileNotFoundException">Thrown if <paramref name="pack"/> does not exist on disk.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="prefix"/> is not rooted.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="pack"/> is null.</exception>
        void MountContentPack(string pack, ResourcePath prefix = null);

        /// <summary>
        ///     Adds a directory to search inside of to the VFS. The directory is relative to
        ///     the executable location on disk.
        /// </summary>
        /// <param name="path">The path of the directory to add to the VFS on disk.</param>
        /// <param name="prefix">The resource path to which all files in the directory will be relative to in the VFS.</param>
        /// <exception cref="DirectoryNotFoundException">Thrown if <paramref name="path"/> does not exist on disk.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="prefix"/> passed is not rooted.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null.</exception>
        void MountContentDirectory(string path, ResourcePath prefix = null);

        /// <summary>
        ///     Read a file from the mounted content roots.
        /// </summary>
        /// <param name="path">The path to the file in the VFS. Must be rooted.</param>
        /// <returns>The memory stream of the file.</returns>
        /// <exception cref="FileNotFoundException">Thrown if <paramref name="path"/> does not exist in the VFS.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is not rooted.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null.</exception>
        MemoryStream ContentFileRead(ResourcePath path);

        /// <summary>
        ///     Read a file from the mounted content roots.
        /// </summary>
        /// <param name="path">The path to the file in the VFS. Must be rooted.</param>
        /// <returns>The memory stream of the file.</returns>
        /// <exception cref="FileNotFoundException">Thrown if <paramref name="path"/> does not exist in the VFS.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is not rooted.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null.</exception>
        MemoryStream ContentFileRead(string path);

        /// <summary>
        ///     Check if a file exists in any of the mounted content roots.
        /// </summary>
        /// <param name="path">The path of the file to check.</param>
        /// <returns>True if the file exists, false otherwise.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is not rooted.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null.</exception>
        bool ContentFileExists(ResourcePath path);

        /// <summary>
        ///     Check if a file exists in any of the mounted content roots.
        /// </summary>
        /// <param name="path">The path of the file to check.</param>
        /// <returns>True if the file exists, false otherwise.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is not rooted.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null.</exception>
        bool ContentFileExists(string path);

        /// <summary>
        ///     Try to read a file from the mounted content roots.
        /// </summary>
        /// <param name="path">The path of the file to try to read.</param>
        /// <param name="fileStream">The memory stream of the file's contents. Null if the file could not be loaded.</param>
        /// <returns>True if the file could be loaded, false otherwise.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is not rooted.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null.</exception>
        bool TryContentFileRead(ResourcePath path, out MemoryStream fileStream);

        /// <summary>
        ///     Try to read a file from the mounted content roots.
        /// </summary>
        /// <param name="path">The path of the file to try to read.</param>
        /// <param name="fileStream">The memory stream of the file's contents. Null if the file could not be loaded.</param>
        /// <returns>True if the file could be loaded, false otherwise.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is not rooted.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null.</exception>
        bool TryContentFileRead(string path, out MemoryStream fileStream);

        /// <summary>
        ///     Recursively finds all files in a directory and all sub directories.
        /// </summary>
        /// <remarks>
        ///     If the directory does not exist, an empty enumerable is returned.
        /// </remarks>
        /// <param name="path">Directory to search inside of.</param>
        /// <returns>Enumeration of all relative file paths of the files found, that is they are relative to <paramref name="path"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is not rooted.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null.</exception>
        IEnumerable<ResourcePath> ContentFindFiles(ResourcePath path);

        /// <summary>
        ///     Recursively finds all files in a directory and all sub directories.
        /// </summary>
        /// <remarks>
        ///     If the directory does not exist, an empty enumerable is returned.
        /// </remarks>
        /// <param name="path">Directory to search inside of.</param>
        /// <returns>Enumeration of all relative file paths of the files found, that is they are relative to <paramref name="path"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is not rooted.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null.</exception>
        IEnumerable<ResourcePath> ContentFindFiles(string path);

        /// <summary>
        ///     TODO: TEMPORARY: We need this because Godot can't load most resources without the disk easily.
        ///     Actually, seems like JetBrains Rider has trouble loading PBD files passed into AppDomain.Load too.
        ///     Hrm.
        /// </summary>
        bool TryGetDiskFilePath(ResourcePath path, out string diskPath);
    }
}
