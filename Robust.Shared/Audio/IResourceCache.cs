using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.ResourceManagement;
using Robust.Shared.Utility;

namespace Robust.Shared.Audio;

/// <summary>
/// Handles caching of <see cref="BaseResource"/>
/// </summary>
public interface IResourceCache
{
    T GetResource<T>(string path, bool useFallback = true)
        where T : BaseResource, new();

    T GetResource<T>(ResPath path, bool useFallback = true)
        where T : BaseResource, new();

    bool TryGetResource<T>(string path, [NotNullWhen(true)] out T? resource)
        where T : BaseResource, new();

    bool TryGetResource<T>(ResPath path, [NotNullWhen(true)] out T? resource)
        where T : BaseResource, new();

    void ReloadResource<T>(string path)
        where T : BaseResource, new();

    void ReloadResource<T>(ResPath path)
        where T : BaseResource, new();

    void CacheResource<T>(string path, T resource)
        where T : BaseResource, new();

    void CacheResource<T>(ResPath path, T resource)
        where T : BaseResource, new();

    T GetFallback<T>()
        where T : BaseResource, new();

    IEnumerable<KeyValuePair<ResPath, T>> GetAllResources<T>() where T : BaseResource, new();
}

