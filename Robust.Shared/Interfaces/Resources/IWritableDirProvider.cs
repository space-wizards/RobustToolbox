using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Robust.Shared.Interfaces.Resources
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

    [PublicAPI]
    public static class WritableDirProviderExt
    {
        /// <summary>
        ///     Opens a file for reading.
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="path">The path of the file to open.</param>
        /// <returns>A valid file stream.</returns>
        /// <exception cref="FileNotFoundException">
        ///     Thrown if the file does not exist.
        /// </exception>
        public static Stream OpenRead(this IWritableDirProvider provider, ResourcePath path)
        {
            return provider.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        /// <summary>
        ///     Opens a file for writing.
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="path">The path of the file to open.</param>
        /// <returns>A valid file stream.</returns>
        public static Stream OpenWrite(this IWritableDirProvider provider, ResourcePath path)
        {
            return provider.Open(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
        }

        /// <returns>A valid file stream.</returns>
        public static Stream Create(this IWritableDirProvider provider, ResourcePath path)
        {
            return provider.Open(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        }

        /// <summary>
        /// Appends a string to the end of a file. If the file does not
        /// exist, creates it.
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="path">Path of file to append to.</param>
        /// <param name="content">String to append.</param>
        public static void AppendAllText(this IWritableDirProvider provider, ResourcePath path, ReadOnlySpan<char> content)
        {
            using var stream = provider.Open(path, FileMode.Append, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(stream, EncodingHelpers.UTF8);

            writer.Write(content);
        }

        /// <summary>
        /// Reads the entire contents of a file to a string.
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="path">File to read.</param>
        /// <returns>String of the file contents, or null if the file could not be read.</returns>
        public static string ReadAllText(this IWritableDirProvider provider, ResourcePath path)
        {
            using var stream = provider.OpenRead(path);
            using var reader = new StreamReader(stream, EncodingHelpers.UTF8);

            return reader.ReadToEnd();
        }

        public static byte[] ReadAllBytes(this IWritableDirProvider provider, ResourcePath path)
        {
            using var stream = provider.OpenRead(path);
            using var memoryStream = new MemoryStream((int) stream.Length);

            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }

        /// <summary>
        /// Writes the content string to a file. If the file exists, its existing contents will
        /// be replaced.
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="path">Path of the file to write to.</param>
        /// <param name="content">String contents of the file.</param>
        public static void WriteAllText(this IWritableDirProvider provider, ResourcePath path, ReadOnlySpan<char> content)
        {
            using var stream = provider.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(stream, EncodingHelpers.UTF8);

            writer.Write(content);
        }

        public static void WriteAllBytes(this IWritableDirProvider provider, ResourcePath path, ReadOnlySpan<byte> content)
        {
            using var stream = provider.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);

            stream.Write(content);
        }
    }
}
