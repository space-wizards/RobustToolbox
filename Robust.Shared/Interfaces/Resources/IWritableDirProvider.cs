using System.Collections.Generic;
using System.IO;
using System.Text;
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
        /// <param name="path">File path to open.</param>
        /// <param name="fileMode">Options on how to open the file.</param>
        /// <returns>A valid file stream, or null if the file could not be opened.</returns>
        Stream Open(ResourcePath path, FileMode fileMode);

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
        /// Appends a string to the end of a file. If the file does not
        /// exist, creates it.
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="path">Path of file to append to.</param>
        /// <param name="content">String to append.</param>
        public static void Append(this IWritableDirProvider provider, ResourcePath path, string content)
        {
            using (var stream = provider.Open(path, FileMode.Append))
            using (var writer = new StreamWriter(stream, EncodingHelpers.UTF8))
            {
                writer.Write(content);
            }
        }

        /// <summary>
        /// Reads the entire contents of a file to a string.
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="path">File to read.</param>
        /// <returns>String of the file contents, or null if the file could not be read.</returns>
        public static string Read(this IWritableDirProvider provider, ResourcePath path)
        {
            using (var stream = provider.Open(path, FileMode.Open))
            using (var reader = new StreamReader(stream, EncodingHelpers.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        public static byte[] ReadBytes(this IWritableDirProvider provider, ResourcePath path)
        {
            using (var stream = provider.Open(path, FileMode.Open))
            using (var memoryStream = new MemoryStream((int) stream.Length))
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Writes the content string to a file. If the file exists, its existing contents will
        /// be replaced.
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="path">Path of the file to write to.</param>
        /// <param name="content">String contents of the file.</param>
        public static void Write(this IWritableDirProvider provider, ResourcePath path, string content)
        {
            using (var stream = provider.Open(path, FileMode.Create))
            using (var writer = new StreamWriter(stream, EncodingHelpers.UTF8))
            {
                writer.Write(content);
            }
        }

        public static void WriteBytes(this IWritableDirProvider provider, ResourcePath path, byte[] content)
        {
            using (var stream = provider.Open(path, FileMode.Create))
            {
                stream.Write(content, 0, content.Length);
            }
        }
    }
}
