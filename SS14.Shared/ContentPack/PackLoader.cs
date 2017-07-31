using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using SS14.Shared.Log;

namespace SS14.Shared.ContentPack
{
    /// <summary>
    ///     Loads a zipped content pack into the VFS.
    /// </summary>
    internal class PackLoader : IContentRoot
    {
        private readonly FileInfo _pack;
        private readonly string _password;
        private ZipFile _zip;

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="pack">The zip file to mount in the VFS.</param>
        /// <param name="password">Optional password to unlock to zip file.</param>
        public PackLoader(FileInfo pack, string password = null)
        {
            _pack = pack;
            _password = password;
        }

        /// <inheritdoc />
        public bool Mount()
        {
            Logger.Info($"[RES] Loading ContentPack: {_pack.FullName}...");

            var zipFileStream = File.OpenRead(_pack.FullName);
            _zip = new ZipFile(zipFileStream);

            if (!string.IsNullOrWhiteSpace(_password))
                _zip.Password = _password;

            return true;
        }

        /// <inheritdoc />
        public MemoryStream GetFile(string relPath)
        {
            var entry = _zip.GetEntry(relPath);

            if (entry == null)
                return null;

            // this caches the deflated entry stream in memory
            // this way people can read the stream however many times they want to,
            // without the performance hit of deflating it every time.
            var memStream = new MemoryStream();
            using (var zipStream = _zip.GetInputStream(entry))
            {
                zipStream.CopyTo(memStream);
                memStream.Position = 0;
            }

            return memStream;
        }
    }
}
