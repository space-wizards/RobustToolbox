using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Robust.Shared.Utility;

namespace Robust.Shared.ContentPack
{
    internal partial class ResourceManager
    {
        private class SingleStreamLoader : IContentRoot
        {
            private readonly MemoryStream _stream;
            private readonly ResourcePath _resourcePath;

            public SingleStreamLoader(MemoryStream stream, ResourcePath resourcePath)
            {
                _stream = stream;
                _resourcePath = resourcePath;
            }

            public void Mount()
            {
                // Nothing to do here I'm pretty sure.
            }

            public bool TryGetFile(ResourcePath relPath, [NotNullWhen(true)] out Stream? stream)
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

            public IEnumerable<ResourcePath> FindFiles(ResourcePath path)
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
