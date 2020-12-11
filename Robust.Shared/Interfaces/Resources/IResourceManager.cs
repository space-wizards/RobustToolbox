using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Interfaces.Resources
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
        ///     Read a file from the mounted content roots.
        /// </summary>
        /// <param name="path">The path to the file in the VFS. Must be rooted.</param>
        /// <returns>The memory stream of the file.</returns>
        /// <exception cref="FileNotFoundException">Thrown if <paramref name="path"/> does not exist in the VFS.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is not rooted.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null.</exception>
        /// <seealso cref="ResourceManagerExt.ContentFileReadOrNull"/>
        Stream ContentFileRead(ResourcePath path);

        /// <summary>
        ///     Read a file from the mounted content roots.
        /// </summary>
        /// <param name="path">The path to the file in the VFS. Must be rooted.</param>
        /// <returns>The memory stream of the file.</returns>
        /// <exception cref="FileNotFoundException">Thrown if <paramref name="path"/> does not exist in the VFS.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is not rooted.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null.</exception>
        Stream ContentFileRead(string path);

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
        /// <seealso cref="ResourceManagerExt.ContentFileReadOrNull"/>
        bool TryContentFileRead(ResourcePath path, [NotNullWhen(true)] out Stream? fileStream);

        /// <summary>
        ///     Try to read a file from the mounted content roots.
        /// </summary>
        /// <param name="path">The path of the file to try to read.</param>
        /// <param name="fileStream">The memory stream of the file's contents. Null if the file could not be loaded.</param>
        /// <returns>True if the file could be loaded, false otherwise.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is not rooted.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null.</exception>
        bool TryContentFileRead(string path, [NotNullWhen(true)] out Stream? fileStream);

        /// <summary>
        ///     Recursively finds all files in a directory and all sub directories.
        /// </summary>
        /// <remarks>
        ///     If the directory does not exist, an empty enumerable is returned.
        /// </remarks>
        /// <param name="path">Directory to search inside of.</param>
        /// <returns>Enumeration of all absolute file paths of the files found.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is not rooted.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null.</exception>
        IEnumerable<ResourcePath> ContentFindFiles(ResourcePath path);

        IEnumerable<ResourcePath> ContentFindRelativeFiles(ResourcePath path)
        {
            foreach (var absPath in ContentFindFiles(path))
            {
                if (!absPath.TryRelativeTo(path, out var rel))
                {
                    DebugTools.Assert("Past must be relative to be returned, unreachable.");
                    throw new InvalidOperationException("This is unreachable");
                }

                yield return rel;
            }
        }

        /// <summary>
        ///     Recursively finds all files in a directory and all sub directories.
        /// </summary>
        /// <remarks>
        ///     If the directory does not exist, an empty enumerable is returned.
        /// </remarks>
        /// <param name="path">Directory to search inside of.</param>
        /// <returns>Enumeration of all absolute file paths of the files found.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is not rooted.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null.</exception>
        IEnumerable<ResourcePath> ContentFindFiles(string path);

        /// <summary>
        ///     Read a file from the mounted content paths to a string.
        /// </summary>
        /// <param name="path">Path of the file to read.</param>
        string ContentFileReadAllText(string path)
        {
            return ContentFileReadAllText(new ResourcePath(path));
        }

        /// <summary>
        ///     Read a file from the mounted content paths to a string.
        /// </summary>
        /// <param name="path">Path of the file to read.</param>
        string ContentFileReadAllText(ResourcePath path)
        {
            return ContentFileReadAllText(path, EncodingHelpers.UTF8);
        }

        /// <summary>
        ///     Read a file from the mounted content paths to a string.
        /// </summary>
        /// <param name="path">Path of the file to read.</param>
        /// <param name="encoding">Text encoding to use when reading.</param>
        string ContentFileReadAllText(ResourcePath path, Encoding encoding)
        {
            using var stream = ContentFileRead(path);
            using var reader = new StreamReader(stream, encoding);

            return reader.ReadToEnd();
        }

        public YamlStream ContentFileReadYaml(ResourcePath path)
        {
            using var reader = ContentFileReadText(path);

            var yamlStream = new YamlStream();
            yamlStream.Load(reader);

            return yamlStream;
        }

        public StreamReader ContentFileReadText(ResourcePath path)
        {
            return ContentFileReadText(path, EncodingHelpers.UTF8);
        }

        public StreamReader ContentFileReadText(ResourcePath path, Encoding encoding)
        {
            var stream = ContentFileRead(path);
            return new StreamReader(stream, encoding);
        }
    }

    internal interface IResourceManagerInternal : IResourceManager
    {
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
