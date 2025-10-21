using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Robust.Client.ResourceManagement;

/// <summary>
/// Handles caching of <see cref="BaseResource"/>
/// </summary>
public interface IResourceCache : IResourceManager
{
    T GetResource<T>(string path, bool useFallback = true)
        where T : BaseResource, new();

    T GetResource<T>(ResPath path, bool useFallback = true)
        where T : BaseResource, new();

    bool TryGetResource<T>(string path, [NotNullWhen(true)] out T? resource)
        where T : BaseResource, new();

    bool TryGetResource<T>(ResPath path, [NotNullWhen(true)] out T? resource)
        where T : BaseResource, new();

    bool TryGetResource(AudioStream stream, [NotNullWhen(true)] out AudioResource? resource);

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

    // Resource load callbacks so content can hook stuff like click maps.
    event Action<TextureLoadedEventArgs> OnRawTextureLoaded;
    event Action<RsiLoadedEventArgs> OnRsiLoaded;

    [Obsolete("Fetch these through IoC directly instead")]
    IClyde Clyde { get; }

    [Obsolete("Fetch these through IoC directly instead")]
    IFontManager FontManager { get; }
}

