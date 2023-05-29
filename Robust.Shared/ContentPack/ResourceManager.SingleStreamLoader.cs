using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Robust.Shared.Utility;

namespace Robust.Shared.ContentPack
{
    internal partial class ResourceManager
    {
        private sealed class SingleStreamLoader : IContentRoot
        {
            private readonly MemoryStream _stream;
            private readonly ResPath _resourcePath;

            public SingleStreamLoader(MemoryStream stream, ResPath resourcePath)
            {
                _stream = stream;
                _resourcePath = resourcePath;
            }

            public void Mount()
            {
                // Nothing to do here I'm pretty sure.
            }

            public bool TryGetFile(ResPath relPath, [NotNullWhen(true)] out Stream? stream)
            {
                if (relPath == _resourcePath)
                {
                    stream = new MemoryStream();
                    lock (_stream)
                    {
                        _stream.CopyTo(stream);
                    }
                    stream.Position = 0;
                    return true;
                }

                stream = default;
                return false;
            }

            public bool FileExists(ResPath relPath)
                => relPath == _resourcePath;

            public IEnumerable<ResPath> FindFiles(ResPath path)
            {
                if (_resourcePath.TryRelativeTo(path, out var relative))
                {
                    yield return _resourcePath;
                }
            }

            public IEnumerable<string> GetRelativeFilePaths()
            {
                yield return _resourcePath.ToString();
            }
        }
    }
}
