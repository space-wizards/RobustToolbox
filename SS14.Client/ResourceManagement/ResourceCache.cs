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
    public class ResourceCache : ResourceManager, IResourceCache, IDisposable
    {
        private Dictionary<(string, Type), BaseResource> CachedResources = new Dictionary<(string, Type), BaseResource>();

        public void LoadBaseResources()
        {
            Initialize();

            // TODO: Godot RIGHT NOW doesn't make it easy to load files from non-disk.
            // AFAICT Godot has an internal system for this (PackedData/PackSource) but it's not exposed enough for us to use it at the moment.
            // Specifically: Godot does use its pack system for exported projects, but there's no way to load new packs at runtime manually.
            // So we wing it with file paths right now.
#if RELEASE
            MountContentDirectory(@"Resources/");
#else
            MountContentDirectory(@"../../Resources/");
            MountContentDirectory(@"Resources/Assemblies", "Assemblies/");
#endif
            //_resources.MountContentPack(@"./EngineContentPack.zip");
        }

        public void LoadLocalResources()
        {
            //_resources.MountDefaultContentPack();
        }

        public T GetResource<T>(string path, bool useFallback = true) where T : BaseResource, new()
        {
            if (CachedResources.TryGetValue((path, typeof(T)), out var cached))
            {
                return (T)cached;
            }

            var _resource = new T();
            try
            {
                if (!TryGetDiskFilePath(path, out var diskPath))
                {
                    throw new FileNotFoundException(path);
                }
                _resource.Load(this, diskPath);
                CachedResources[(path, typeof(T))] = _resource;
                return _resource;
            }
            catch (Exception e)
            {
                if (useFallback && _resource.Fallback != null)
                {
                    Logger.Error($"Exception while loading resource {typeof(T)} at '{path}', resorting to fallback.\n{Environment.StackTrace}\n{e}");
                    return GetResource<T>(_resource.Fallback, useFallback = false);
                }
                else
                {
                    Logger.Error($"Exception while loading resource {typeof(T)} at '{path}', no fallback available\n{Environment.StackTrace}\n{e}");
                    throw;
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
                if (!TryGetDiskFilePath(path, out var diskPath))
                {
                    throw new FileNotFoundException(path);
                }
                _resource.Load(this, diskPath);
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

        public T GetFallback<T>() where T : BaseResource, new()
        {
            var res = new T();
            if (res.Fallback == null)
            {
                throw new InvalidOperationException($"Resource of type '{typeof(T)}' has no fallback.");
            }
            return GetResource<T>(res.Fallback, useFallback: false);
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
