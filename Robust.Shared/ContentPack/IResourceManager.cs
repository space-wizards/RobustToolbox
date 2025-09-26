﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.ContentPack
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
        ///     Provides a way to mount a <see cref="IContentRoot"/> implementation to the VFS.
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="loader"></param>
        void AddRoot(ResPath prefix, IContentRoot loader);

        /// <summary>
        ///     Read a file from the mounted content roots.
        /// </summary>
        /// <param name="path">The path to the file in the VFS. Must be rooted.</param>
        /// <returns>The memory stream of the file.</returns>
        /// <exception cref="FileNotFoundException">Thrown if <paramref name="path"/> does not exist in the VFS.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is not rooted.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null.</exception>
        /// <seealso cref="ResourceManagerExt.ContentFileReadOrNull"/>
        Stream ContentFileRead(ResPath path);

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
        bool ContentFileExists(ResPath path);

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
        bool TryContentFileRead(ResPath? path, [NotNullWhen(true)] out Stream? fileStream);

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
        IEnumerable<ResPath> ContentFindFiles(ResPath? path);

        IEnumerable<ResPath> ContentFindRelativeFiles(ResPath path)
        {
            foreach (var absPath in ContentFindFiles(path))
            {
                if (!absPath.TryRelativeTo(path, out var rel))
                {
                    DebugTools.Assert("Past must be relative to be returned, unreachable.");
                    throw new InvalidOperationException("This is unreachable");
                }

                yield return rel.Value;
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
        IEnumerable<ResPath> ContentFindFiles(string path);

        /// <summary>
        /// Gets entries in a content directory.
        /// </summary>
        /// <remarks>
        /// This is not a performant API; the VFS does not work natively with these kinds of directory entries.
        /// This is intended for development tools such as console command completions,
        /// not for general-purpose resource management.
        /// </remarks>
        /// <param name="path"></param>
        /// <returns>A sequence of entry names. If the entry name ends in a slash, it's a directory.</returns>
        IEnumerable<string> ContentGetDirectoryEntries(ResPath path);

        /// <summary>
        ///     Returns a list of paths to all top-level content directories
        /// </summary>
        /// <returns></returns>
        [Obsolete("This API is no longer content-accessible")]
        IEnumerable<ResPath> GetContentRoots();

        /// <summary>
        ///     Read a file from the mounted content paths to a string.
        /// </summary>
        /// <param name="path">Path of the file to read.</param>
        string ContentFileReadAllText(string path)
        {
            return ContentFileReadAllText(new ResPath(path));
        }

        /// <summary>
        ///     Read a file from the mounted content paths to a string.
        /// </summary>
        /// <param name="path">Path of the file to read.</param>
        string ContentFileReadAllText(ResPath path)
        {
            return ContentFileReadAllText(path, EncodingHelpers.UTF8);
        }

        /// <summary>
        ///     Read a file from the mounted content paths to a string.
        /// </summary>
        /// <param name="path">Path of the file to read.</param>
        /// <param name="encoding">Text encoding to use when reading.</param>
        string ContentFileReadAllText(ResPath path, Encoding encoding)
        {
            using var stream = ContentFileRead(path);
            using var reader = new StreamReader(stream, encoding);

            return reader.ReadToEnd();
        }

        public YamlStream ContentFileReadYaml(ResPath path)
        {
            using var reader = ContentFileReadText(path);

            var yamlStream = new YamlStream();
            yamlStream.Load(reader);

            return yamlStream;
        }

        public StreamReader ContentFileReadText(ResPath path)
        {
            return ContentFileReadText(path, EncodingHelpers.UTF8);
        }

        public StreamReader ContentFileReadText(ResPath path, Encoding encoding)
        {
            var stream = ContentFileRead(path);
            return new StreamReader(stream, encoding);
        }
    }
}
