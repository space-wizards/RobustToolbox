using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Robust.LoaderApi;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Robust.Client.ResourceManagement
{
    internal partial class ResourceCache
    {
        private sealed class LoaderApiLoader : IContentRoot
        {
            private readonly IFileApi _api;
            private readonly string _prefix;

            public LoaderApiLoader(IFileApi api, string prefix)
            {
                _api = api;
                _prefix = prefix;
            }

            public void Mount()
            {
            }

            public bool TryGetFile(ResourcePath relPath, [NotNullWhen(true)] out Stream? stream)
            {
                if (_api.TryOpen($"{_prefix}{relPath}", out stream))
                {
                    return true;
                }

                stream = null;
                return false;
            }

            public IEnumerable<ResourcePath> FindFiles(ResourcePath path)
            {
                foreach (var relPath in _api.AllFiles)
                {
                    if (!relPath.StartsWith(_prefix))
                        continue;

                    var resP = new ResourcePath(relPath[_prefix.Length..]);
                    if (resP.TryRelativeTo(path, out _))
                    {
                        yield return resP;
                    }
                }
            }

            public IEnumerable<string> GetRelativeFilePaths()
            {
                return _api.AllFiles;
            }
        }
    }
}
