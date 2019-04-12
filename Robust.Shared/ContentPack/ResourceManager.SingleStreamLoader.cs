using System.Collections.Generic;
using System.IO;
using Robust.Shared.Utility;

namespace Robust.Shared.ContentPack
{
    public partial class ResourceManager
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

            public bool TryGetFile(ResourcePath relPath, out MemoryStream stream)
            {
                System.Console.WriteLine(relPath);
                if (relPath == _resourcePath)
                {
                    stream = new MemoryStream();
                    _stream.CopyTo(stream);
                    stream.Position = 0;
                    return true;
                }

                stream = default;
                return false;
            }

            public IEnumerable<ResourcePath> FindFiles(ResourcePath path)
            {
                if (path.TryRelativeTo(_resourcePath, out var relative))
                {
                    yield return relative;
                }
            }
        }
    }
}
