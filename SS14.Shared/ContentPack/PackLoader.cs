using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using SS14.Shared.Log;
using SS14.Shared.Utility;

namespace SS14.Shared.ContentPack
{
    public partial class ResourceManager
    {
        /// <summary>
        ///     Loads a zipped content pack into the VFS.
        /// </summary>
        class PackLoader : IContentRoot
        {
            private readonly FileInfo _pack;
            private ZipFile _zip;

            /// <summary>
            ///     Constructor.
            /// </summary>
            /// <param name="pack">The zip file to mount in the VFS.</param>
            public PackLoader(FileInfo pack)
            {
                _pack = pack;
            }

            /// <inheritdoc />
            public void Mount()
            {
                Logger.Info($"[RES] Loading ContentPack: {_pack.FullName}...");

                var zipFileStream = File.OpenRead(_pack.FullName);
                _zip = new ZipFile(zipFileStream);
            }

            /// <inheritdoc />
            public bool TryGetFile(ResourcePath relPath, out MemoryStream stream)
            {
                var entry = _zip.GetEntry(relPath.ToRootedPath().ToString());

                if (entry == null)
                {
                    stream = null;
                    return false;
                }

                // this caches the deflated entry stream in memory
                // this way people can read the stream however many times they want to,
                // without the performance hit of deflating it every time.
                stream = new MemoryStream();
                using (var zipStream = _zip.GetInputStream(entry))
                {
                    zipStream.CopyTo(stream);
                    stream.Position = 0;
                }

                return true;
            }

            /// <inheritdoc />
            public IEnumerable<ResourcePath> FindFiles(ResourcePath path)
            {
                foreach (ZipEntry zipEntry in _zip)
                {
                    if (zipEntry.IsFile && zipEntry.Name.StartsWith(path.ToRootedPath().ToString()))
                        yield return new ResourcePath(zipEntry.Name).ToRelativePath();
                }
            }
        }
    }
}
