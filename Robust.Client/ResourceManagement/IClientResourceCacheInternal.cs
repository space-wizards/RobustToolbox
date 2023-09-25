using Robust.LoaderApi;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Robust.Client.ResourceManagement;

/// <inheritdoc />
internal interface IClientResourceCacheInternal : IClientResourceCache
{
    void TextureLoaded(TextureLoadedEventArgs eventArgs);
    void RsiLoaded(RsiLoadedEventArgs eventArgs);
    void PreloadTextures();

    void MountLoaderApi(IResourceManager manager, IFileApi api, string apiPrefix, ResPath? prefix = null);
}
