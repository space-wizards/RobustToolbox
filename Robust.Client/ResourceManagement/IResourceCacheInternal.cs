using Robust.LoaderApi;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Robust.Client.ResourceManagement;

/// <inheritdoc />
internal interface IResourceCacheInternal : IClientResourceCache
{
    void TextureLoaded(TextureLoadedEventArgs eventArgs);
    void RsiLoaded(RsiLoadedEventArgs eventArgs);

    void MountLoaderApi(IFileApi api, string apiPrefix, ResPath? prefix=null);
    void PreloadTextures();
}
