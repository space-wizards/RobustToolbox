using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Robust.Shared.ContentPack
{
    [PublicAPI]
    public static class WritableDirProviderExt
    {
        /// <summary>
        ///     Opens a file for reading.
        /// </summary>
        /// <param name="provider">The writable directory provider</param>
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
        ///     Opens a file for reading.
        /// </summary>
        /// <param name="provider">The writable directory provider</param>
        /// <param name="path">The path of the file to open.</param>
        /// <returns>A valid stream reader.</returns>
        /// <exception cref="FileNotFoundException">
        ///     Thrown if the file does not exist.
        /// </exception>
        public static StreamReader OpenText(this IWritableDirProvider provider, ResourcePath path)
        {
            var stream = OpenRead(provider, path);
            return new StreamReader(stream, EncodingHelpers.UTF8);
        }

        /// <summary>
        ///     Opens a file for writing. If the file already exists, it will be overwritten.
        /// </summary>
        /// <param name="provider">The writable directory provider</param>
        /// <param name="path">The path of the file to open.</param>
        /// <returns>A valid file stream.</returns>
        public static Stream OpenWrite(this IWritableDirProvider provider, ResourcePath path)
        {
            return provider.Open(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        }

        /// <summary>
        ///     Opens a file for writing. If the file already exists, it will be overwritten.
        /// </summary>
        /// <param name="provider">The writable directory provider</param>
        /// <param name="path">The path of the file to open.</param>
        /// <returns>A valid file stream writer.</returns>
        public static StreamWriter OpenWriteText(this IWritableDirProvider provider, ResourcePath path)
        {
            var stream = OpenWrite(provider, path);
            return new StreamWriter(stream, EncodingHelpers.UTF8);
        }

        /// <summary>
        /// Appends a string to the end of a file. If the file does not
        /// exist, creates it.
        /// </summary>
        /// <param name="provider">The writable directory provider</param>
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
        /// <returns>String of the file contents</returns>
        public static string ReadAllText(this IWritableDirProvider provider, ResourcePath path)
        {
            using var reader = provider.OpenText(path);

            return reader.ReadToEnd();
        }

        /// <summary>
        /// Reads the entire contents of a path to a string.
        /// </summary>
        /// <param name="provider">The writable directory to look for the path in.</param>
        /// <param name="path">The path to read the contents from.</param>
        /// <param name="text">The content read from the path, or null if the path did not exist.</param>
        /// <returns>true if path was successfully read; otherwise, false.</returns>
        public static bool TryReadAllText(this IWritableDirProvider provider, ResourcePath path, [NotNullWhen(true)] out string? text)
        {
            try
            {
                text = ReadAllText(provider, path);
                return true;
            }
            catch(FileNotFoundException)
            {
                text = null;
                return false;
            }
        }

        /// <summary>
        /// Reads the entire contents of a path to a byte array.
        /// </summary>
        /// <param name="provider">The writable directory to look for the path in.</param>
        /// <param name="path">The path to read the contents from.</param>
        /// <returns>The contents of the path as a byte array.</returns>
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
        /// <param name="provider">The writable directory provider</param>
        /// <param name="path">Path of the file to write to.</param>
        /// <param name="content">String contents of the file.</param>
        public static void WriteAllText(this IWritableDirProvider provider, ResourcePath path, ReadOnlySpan<char> content)
        {
            using var writer = provider.OpenWriteText(path);

            writer.Write(content);
        }

        /// <summary>
        /// Writes the sequence of bytes to a file. If the file exists, its existing contents will
        /// be replaced.
        /// </summary>
        /// <param name="provider">The writable directory provider</param>
        /// <param name="path">Path of the file to write to.</param>
        /// <param name="content">Bytes to write to the file.</param>
        public static void WriteAllBytes(this IWritableDirProvider provider, ResourcePath path, ReadOnlySpan<byte> content)
        {
            using var stream = provider.OpenWrite(path);

            stream.Write(content);
        }
    }
}
