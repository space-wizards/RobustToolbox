using System;
using System.IO;
using System.IO.Compression;

namespace SS14.Shared.Utility
{
    /// <summary>
    ///  <para>Provides some quick-and-dirty methods to deflate utf8 strings.</para>
    /// </summary>
    public static class ZipString
    {
        /// <summary>
        ///  <para>Compress the given string into a byte array.</para>
        /// </summary>
        public static byte[] ZipStr(String str)
        {
            using (MemoryStream output = new MemoryStream())
            {
                using (DeflateStream gzip =
                  new DeflateStream(output, CompressionMode.Compress))
                {
                    using (StreamWriter writer =
                      new StreamWriter(gzip, System.Text.Encoding.UTF8))
                    {
                        writer.Write(str);
                    }
                }

                return output.ToArray();
            }
        }

        /// <summary>
        ///  <para>Decompress the given byte array into a string.</para>
        /// </summary>
        public static string UnZipStr(byte[] input)
        {
            using (MemoryStream inputStream = new MemoryStream(input))
            {
                using (DeflateStream gzip =
                  new DeflateStream(inputStream, CompressionMode.Decompress))
                {
                    using (StreamReader reader =
                      new StreamReader(gzip, System.Text.Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }
    }
}
