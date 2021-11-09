using System;
using System.IO;

namespace Robust.Shared.Utility
{
    /// <summary>
    ///     Extension methods for working with streams.
    /// </summary>
    public static class StreamExt
    {
        /// <summary>
        ///     Copies any stream into a byte array.
        /// </summary>
        /// <param name="stream">The stream to copy.</param>
        /// <returns>The byte array.</returns>
        public static byte[] CopyToArray(this Stream stream)
        {
            using (var memStream = new MemoryStream())
            {
                stream.CopyTo(memStream);
                return memStream.ToArray();
            }
        }

        /// <exception cref="EndOfStreamException">
        /// Thrown if not exactly <paramref name="amount"/> bytes could be read.
        /// </exception>
        public static byte[] ReadExact(this Stream stream, int amount)
        {
            var buffer = new byte[amount];
            var read = 0;
            while (read < amount)
            {
                var cRead = stream.Read(buffer, read, amount - read);
                if (cRead == 0)
                {
                    throw new EndOfStreamException();
                }

                read += cRead;
            }

            return buffer;
        }

        /// <exception cref="EndOfStreamException">
        /// Thrown if not exactly <paramref name="buffer.Length"/> bytes could be read.
        /// </exception>
        public static void ReadExact(this Stream stream, Span<byte> buffer)
        {
            while (buffer.Length > 0)
            {
                var cRead = stream.Read(buffer);
                if (cRead == 0)
                    throw new EndOfStreamException();

                buffer = buffer[cRead..];
            }
        }

        public static int ReadToEnd(this Stream stream, Span<byte> buffer)
        {
            var totalRead = 0;
            while (true)
            {
                var read = stream.Read(buffer);
                totalRead += read;
                if (read == 0)
                    return totalRead;

                buffer = buffer[read..];
            }
        }

        public static int ReadToEnd(this Stream stream, byte[] buffer)
        {
            var totalRead = 0;
            while (true)
            {
                var read = stream.Read(buffer, totalRead, buffer.Length - totalRead);
                totalRead += read;
                if (read == 0)
                    return totalRead;
            }
        }
    }
}
