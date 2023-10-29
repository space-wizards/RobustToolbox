using System;
using Robust.Shared.ResourceManagement;

namespace Robust.Client.ResourceManagement;

/// <summary>
/// Handles caching of <see cref="BaseResource"/>
/// </summary>
internal sealed partial class ResourceCache : SharedResourceCache, IClientResourceCacheInternal, IDisposable
{
    public event Action<TextureLoadedEventArgs>? OnRawTextureLoaded;
    public event Action<RsiLoadedEventArgs>? OnRsiLoaded;

    public void TextureLoaded(TextureLoadedEventArgs eventArgs)
    {
        OnRawTextureLoaded?.Invoke(eventArgs);
    }

    public void RsiLoaded(RsiLoadedEventArgs eventArgs)
    {
        OnRsiLoaded?.Invoke(eventArgs);
    }
}
