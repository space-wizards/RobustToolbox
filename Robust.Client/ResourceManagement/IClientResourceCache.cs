using System;
using Robust.Client.Graphics;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;

namespace Robust.Client.ResourceManagement;

/// <inheritdoc />
public interface IClientResourceCache : IResourceCache
{
    // Resource load callbacks so content can hook stuff like click maps.
    event Action<TextureLoadedEventArgs> OnRawTextureLoaded;
    event Action<RsiLoadedEventArgs> OnRsiLoaded;

    IClyde Clyde { get; }
    IFontManager FontManager { get; }
}
