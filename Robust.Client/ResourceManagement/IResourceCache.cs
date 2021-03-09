using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Robust.Client.ResourceManagement
{
    public interface IResourceCache : IResourceManager
    {
        T GetResource<T>(string path, bool useFallback = true)
            where T : BaseResource, new();

        T GetResource<T>(ResourcePath path, bool useFallback = true)
            where T : BaseResource, new();

        bool TryGetResource<T>(string path, [NotNullWhen(true)] out T? resource)
            where T : BaseResource, new();

        bool TryGetResource<T>(ResourcePath path, [NotNullWhen(true)] out T? resource)
            where T : BaseResource, new();

        void ReloadResource<T>(string path)
            where T : BaseResource, new();

        void ReloadResource<T>(ResourcePath path)
            where T : BaseResource, new();

        void CacheResource<T>(string path, T resource)
            where T : BaseResource, new();

        void CacheResource<T>(ResourcePath path, T resource)
            where T : BaseResource, new();

        T GetFallback<T>()
            where T : BaseResource, new();

        IEnumerable<KeyValuePair<ResourcePath, T>> GetAllResources<T>() where T : BaseResource, new();

        // Resource load callbacks so content can hook stuff like click maps.
        event Action<TextureLoadedEventArgs> OnRawTextureLoaded;
        event Action<RsiLoadedEventArgs> OnRsiLoaded;
    }
}