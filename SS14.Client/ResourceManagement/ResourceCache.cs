using SS14.Client.Interfaces.ResourceManagement;
using SS14.Shared.ContentPack;
using SS14.Shared.Interfaces;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SS14.Client.ResourceManagement;
using SS14.Shared.Configuration;

namespace SS14.Client.ResourceManagement
{
    public class ResourceCache : IResourceCache, IDisposable
    {
        [Dependency]
        readonly IResourceManager _resources;

        private static readonly string BaseResourceDir = PathHelpers.ExecutableRelativeFile(@"../../Resources/");
        private Dictionary<(string, Type), BaseResource> CachedResources = new Dictionary<(string, Type), BaseResource>();

        public void LoadBaseResources()
        {
            _resources.Initialize();

            // TODO: Right now the resource cache doesn't use the VFS,
            //   so that we always have on-disk locations of files.
            //   Godot doesn't make it easy to load resources without them being on-disk.
#if RELEASE
            _resources.MountContentDirectory(@"./Resources/");
#else
            _resources.MountContentDirectory(@"../../Resources/");
#endif
            //_resources.MountContentPack(@"./EngineContentPack.zip");
        }

        public void LoadLocalResources()
        {
            //_resources.MountDefaultContentPack();
        }

        public T GetResource<T>(string path) where T : BaseResource, new()
        {
            if (CachedResources.TryGetValue((path, typeof(T)), out var cached))
            {
                return (T)cached;
            }

            var _resource = new T();
            try
            {
                _resource.Load(this, Path.Combine(BaseResourceDir, path));
                CachedResources[(path, typeof(T))] = _resource;
                return _resource;
            }
            catch (Exception)
            {
                if (_resource.Fallback != null)
                {
                    // TODO: This totally infinite loops if the fallback throws an exception too.
                    Logger.Error($"Exception while loading resource {typeof(T)} at '{path}', resorting to fallback.");
                    return GetResource<T>(_resource.Fallback);
                }
                else
                {
                    // No exception logs because of how insanely spammy it gets.
                    Logger.Error($"Exception while loading resource {typeof(T)} at '{path}', no fallback available\n{Environment.StackTrace}");
                    return null;
                }
            }
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
            catch
            {
                resource = null;
                return false;
            }
        }

        public bool HasResource<T>(string path) where T : BaseResource, new()
        {
            return TryGetResource<T>(path, out var _);
        }

        public void CacheResource<T>(string path, T resource) where T : BaseResource, new()
        {
            CachedResources[(path, typeof(T))] = resource;
        }

        #region IDisposable Members

        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                foreach (var res in CachedResources.Values)
                {
                    res.Dispose();
                }
            }

            disposed = true;
        }

        ~ResourceCache()
        {
            Dispose(false);
        }

        #endregion IDisposable Members
    }
}
