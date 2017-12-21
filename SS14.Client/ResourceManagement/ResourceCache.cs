using SS14.Client.Interfaces.ResourceManagement;
using SS14.Shared.Interfaces;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Shared.ContentPack;
using System.IO;
using SS14.Shared.Log;

namespace SS14.Client.ResourceManagement
{
    public class ResourceCache : IResourceCache
    {
        [Dependency]
        readonly IResourceManager _resources;

        private static readonly string BaseResourceDir = PathHelpers.ExecutableRelativeFile(@"./Resources/");
        private Dictionary<(string, Type), BaseResource> CachedResources = new Dictionary<(string, Type), BaseResource>();

        public void LoadBaseResources()
        {
            _resources.Initialize();

            // TODO: Right now the resource cache doesn't use the VFS,
            //   so that we always have on-disk locations of files.
            //   Godot doesn't make it easy to load resources without them being on-disk.
            _resources.MountContentDirectory(@"./Resources/");
            _resources.MountContentPack(@"./EngineContentPack.zip");
        }

        public void LoadLocalResources()
        {
            _resources.MountDefaultContentPack();
        }

        public T GetResource<T>(string path) where T : BaseResource, new()
        {
            TryGetResource(path, out T res);
            return res;
        }

        public bool TryGetResource<T>(string path, out T resource) where T : BaseResource, new()
        {
            if (CachedResources.TryGetValue((path, typeof(T)), out var cached))
            {
                resource = (T)cached;
                return true;
            }
            var _resource = new T();
            try
            {
                _resource.Load(this, Path.Combine(BaseResourceDir, path));
                resource = _resource;
                CachedResources[(path, typeof(T))] = resource;
                return true;
            }
            catch (Exception e)
            {
                if (_resource.Fallback != null)
                {
                    // TODO: This totally infinite loops if the fallback throws an exception too.
                    Logger.Error($"Exception while loading resource {typeof(T)} at '{path}', resorting to fallback:\n{e}");
                    return TryGetResource(_resource.Fallback, out resource);
                }
                else
                {
                    resource = null;
                    return false;
                }
            }
        }

        public void CacheResource<T>(string path, T resource) where T : BaseResource, new()
        {
            CachedResources[(path, typeof(T))] = resource;
        }
    }
}
