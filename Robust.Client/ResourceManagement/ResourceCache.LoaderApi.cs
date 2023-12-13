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
        public void MountLoaderApi(IResourceManager manager, IFileApi api, string apiPrefix, ResPath? prefix = null)
        {
            prefix ??= ResPath.Root;
            var root = new LoaderApiLoader(api, apiPrefix);
            manager.AddRoot(prefix.Value, root);
        }

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

            public bool TryGetFile(ResPath relPath, [NotNullWhen(true)] out Stream? stream)
            {
                if (_api.TryOpen($"{_prefix}{relPath}", out stream))
                {
                    return true;
                }

                stream = null;
                return false;
            }

            public bool FileExists(ResPath relPath)
            {
                return _api.TryOpen($"{_prefix}{relPath}", out _);
            }

            public IEnumerable<ResPath> FindFiles(ResPath path)
            {
                foreach (var relPath in _api.AllFiles)
                {
                    if (!relPath.StartsWith(_prefix))
                        continue;

                    var resP = new ResPath(relPath[_prefix.Length..]);
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
