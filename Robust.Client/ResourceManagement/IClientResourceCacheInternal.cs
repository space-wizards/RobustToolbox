namespace Robust.Client.ResourceManagement;

/// <inheritdoc />
internal interface IClientResourceCacheInternal : IClientResourceCache
{
    void TextureLoaded(TextureLoadedEventArgs eventArgs);
    void RsiLoaded(RsiLoadedEventArgs eventArgs);
    void PreloadTextures();
}
