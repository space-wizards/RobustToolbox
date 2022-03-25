using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Robust.Shared.Utility;

namespace Robust.Shared.ContentPack
{
    internal interface IResourceManagerInternal : IResourceManager
    {
        /// <summary>
        ///     Fires with the newly added root in <see cref="IResourceManager.AddRoot"/>.
        /// </summary>
        event Action<IContentRoot>? RootAdded;

        /// <summary>
        ///     Sets the manager up so that the base game can run.
        /// </summary>
        /// <param name="userData">
        /// The directory to use for user data.
        /// If null, a virtual temporary file system is used instead.
        /// </param>
        void Initialize(string? userData);

        /// <summary>
        ///     Mounts a single stream as a content file. Useful for unit testing.
        /// </summary>
        /// <param name="stream">The stream to mount.</param>
        /// <param name="path">The path that the file will be mounted at.</param>
        void MountStreamAt(MemoryStream stream, ResourcePath path);

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
        void MountContentPack(string pack, ResourcePath? prefix = null);

        void MountContentPack(Stream zipStream, ResourcePath? prefix = null);

        /// <summary>
        ///     Adds a directory to search inside of to the VFS. The directory is relative to
        ///     the executable location on disk.
        /// </summary>
        /// <param name="path">The path of the directory to add to the VFS on disk.</param>
        /// <param name="prefix">The resource path to which all files in the directory will be relative to in the VFS.</param>
        /// <exception cref="DirectoryNotFoundException">Thrown if <paramref name="path"/> does not exist on disk.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="prefix"/> passed is not rooted.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null.</exception>
        void MountContentDirectory(string path, ResourcePath? prefix = null);

        /// <summary>
        ///     Attempts to get an on-disk path absolute file path for the specified resource path.
        /// </summary>
        /// <remarks>
        /// <para>
        ///     This only works if the resource is mounted as a direct directory,
        ///     so this obviously fails if the resource is mounted in another way such as a zip file.
        /// </para>
        /// <para>
        ///     This can be used for optimizations such as assembly loading, where an on-disk path is better.
        /// </para>
        /// </remarks>
        bool TryGetDiskFilePath(ResourcePath path, [NotNullWhen(true)] out string? diskPath);
    }
}
