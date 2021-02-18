using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Shared.ContentPack
{
    internal partial class ResourceManager
    {
        /// <summary>
        ///     Loads a zipped content pack into the VFS.
        /// </summary>
        class PackLoader : IContentRoot
        {
            private readonly FileInfo? _pack;
            private readonly Stream? _stream;
            private ZipArchive _zip = default!;

            /// <summary>
            ///     Constructor.
            /// </summary>
            /// <param name="pack">The zip file to mount in the VFS.</param>
            public PackLoader(FileInfo pack)
            {
                _pack = pack;
            }

            public PackLoader(Stream stream)
            {
                _stream = stream;
            }

            /// <inheritdoc />
            public void Mount()
            {
                if (_pack != null)
                {
                    Logger.InfoS("res", $"Loading ContentPack: {_pack.FullName}...");

                    _zip = ZipFile.OpenRead(_pack.FullName);
                }
                else
                {
                    // Stream constructor.
                    DebugTools.AssertNotNull(_stream);

                    _zip = new ZipArchive(_stream!, ZipArchiveMode.Read);
                }
            }

            /// <inheritdoc />
            public bool TryGetFile(ResourcePath relPath, [NotNullWhen(true)] out Stream? stream)
            {
                var entry = _zip.GetEntry(relPath.ToString());

                if (entry == null)
                {
                    stream = null;
                    return false;
                }

                // this caches the deflated entry stream in memory
                // this way people can read the stream however many times they want to,
                // without the performance hit of deflating it every time.
                stream = new MemoryStream();
                lock (_zip)
                {
                    using var zipStream = entry.Open();
                    zipStream.CopyTo(stream);
                }

                stream.Position = 0;
                return true;
            }

            /// <inheritdoc />
            public IEnumerable<ResourcePath> FindFiles(ResourcePath path)
            {
                var rootPath = path + "/";
                foreach (var entry in _zip.Entries)
                {
                    if (entry.Name == "")
                    {
                        // Dir node.
                        continue;
                    }

                    if (entry.FullName.StartsWith(rootPath))
                    {
                        yield return new ResourcePath(entry.FullName).ToRelativePath();
                    }
                }
            }

            public IEnumerable<string> GetRelativeFilePaths()
            {
                foreach (var entry in _zip.Entries)
                {
                    if (entry.Name == "")
                    {
                        // Dir node.
                        continue;
                    }

                    yield return new ResourcePath(entry.FullName).ToRootedPath().ToString();
                }
            }
        }
    }
}
