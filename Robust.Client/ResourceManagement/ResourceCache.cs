using Robust.Shared.ContentPack;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Robust.LoaderApi;

namespace Robust.Client.ResourceManagement
{
    internal partial class ResourceCache : ResourceManager, IResourceCacheInternal, IDisposable
    {
        private readonly Dictionary<Type, Dictionary<ResourcePath, BaseResource>> CachedResources =
            new();

        private readonly Dictionary<Type, BaseResource> _fallbacks = new();

        public T GetResource<T>(string path, bool useFallback = true) where T : BaseResource, new()
        {
            return GetResource<T>(new ResourcePath(path), useFallback);
        }

        public T GetResource<T>(ResourcePath path, bool useFallback = true) where T : BaseResource, new()
        {
            var cache = GetTypeDict<T>();
            if (cache.TryGetValue(path, out var cached))
            {
                return (T) cached;
            }

            var _resource = new T();
            try
            {
                _resource.Load(this, path);
                cache[path] = _resource;
                return _resource;
            }
            catch (Exception e)
            {
                if (useFallback && _resource.Fallback != null)
                {
                    Logger.Error(
                        $"Exception while loading resource {typeof(T)} at '{path}', resorting to fallback.\n{Environment.StackTrace}\n{e}");
                    return GetResource<T>(_resource.Fallback, false);
                }
                else
                {
                    Logger.Error(
                        $"Exception while loading resource {typeof(T)} at '{path}', no fallback available\n{Environment.StackTrace}\n{e}");
                    throw;
                }
            }
        }

        public bool TryGetResource<T>(string path, [NotNullWhen(true)] out T? resource) where T : BaseResource, new()
        {
            return TryGetResource(new ResourcePath(path), out resource);
        }

        public bool TryGetResource<T>(ResourcePath path, [NotNullWhen(true)] out T? resource) where T : BaseResource, new()
        {
            var cache = GetTypeDict<T>();
            if (cache.TryGetValue(path, out var cached))
            {
                resource = (T) cached;
                return true;
            }

            var _resource = new T();
            try
            {
                _resource.Load(this, path);
                resource = _resource;
                cache[path] = resource;
                return true;
            }
            catch
            {
                resource = null;
                return false;
            }
        }

        public void ReloadResource<T>(string path) where T : BaseResource, new()
        {
            ReloadResource<T>(new ResourcePath(path));
        }

        public void ReloadResource<T>(ResourcePath path) where T : BaseResource, new()
        {
            var cache = GetTypeDict<T>();

            if (!cache.TryGetValue(path, out var res))
            {
                return;
            }

            try
            {
                res.Reload(this, path);
            }
            catch (Exception e)
            {
                Logger.Error($"Exception while reloading resource {typeof(T)} at '{path}'\n{e}");
                throw;
            }
        }

        public bool HasResource<T>(string path) where T : BaseResource, new()
        {
            return HasResource<T>(new ResourcePath(path));
        }

        public bool HasResource<T>(ResourcePath path) where T : BaseResource, new()
        {
            return TryGetResource<T>(path, out var _);
        }

        public void CacheResource<T>(string path, T resource) where T : BaseResource, new()
        {
            CacheResource(new ResourcePath(path), resource);
        }

        public void CacheResource<T>(ResourcePath path, T resource) where T : BaseResource, new()
        {
            GetTypeDict<T>()[path] = resource;
        }

        public T GetFallback<T>() where T : BaseResource, new()
        {
            if (_fallbacks.TryGetValue(typeof(T), out var fallback))
            {
                return (T) fallback;
            }

            var res = new T();
            if (res.Fallback == null)
            {
                throw new InvalidOperationException($"Resource of type '{typeof(T)}' has no fallback.");
            }

            fallback = GetResource<T>(res.Fallback, useFallback: false);
            _fallbacks.Add(typeof(T), fallback);
            return (T) fallback;
        }

        public IEnumerable<KeyValuePair<ResourcePath, T>> GetAllResources<T>() where T : BaseResource, new()
        {
            return GetTypeDict<T>().Select(p => new KeyValuePair<ResourcePath, T>(p.Key, (T) p.Value));
        }

        public event Action<TextureLoadedEventArgs>? OnRawTextureLoaded;
        public event Action<RsiLoadedEventArgs>? OnRsiLoaded;

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
                foreach (var res in CachedResources.Values.SelectMany(dict => dict.Values))
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Dictionary<ResourcePath, BaseResource> GetTypeDict<T>()
        {
            if (!CachedResources.TryGetValue(typeof(T), out var ret))
            {
                ret = new Dictionary<ResourcePath, BaseResource>();
                CachedResources.Add(typeof(T), ret);
            }

            return ret;
        }

        public void TextureLoaded(TextureLoadedEventArgs eventArgs)
        {
            OnRawTextureLoaded?.Invoke(eventArgs);
        }

        public void RsiLoaded(RsiLoadedEventArgs eventArgs)
        {
            OnRsiLoaded?.Invoke(eventArgs);
        }

        public void MountLoaderApi(IFileApi api, string apiPrefix, ResourcePath? prefix=null)
        {
            prefix ??= ResourcePath.Root;
            var root = new LoaderApiLoader(api, apiPrefix);
            AddRoot(prefix, root);
        }
    }
}
