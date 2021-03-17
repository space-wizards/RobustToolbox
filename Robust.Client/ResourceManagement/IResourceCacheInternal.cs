using Robust.LoaderApi;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Robust.Client.ResourceManagement
{
    internal interface IResourceCacheInternal : IResourceCache, IResourceManagerInternal
    {
        void TextureLoaded(TextureLoadedEventArgs eventArgs);
        void RsiLoaded(RsiLoadedEventArgs eventArgs);

        void MountLoaderApi(IFileApi api, string apiPrefix, ResourcePath? prefix=null);
        void PreloadTextures();
    }
}
