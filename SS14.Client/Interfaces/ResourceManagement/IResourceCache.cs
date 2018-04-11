using SS14.Client.ResourceManagement;
using SS14.Shared.Interfaces;
using SS14.Shared.Utility;

namespace SS14.Client.Interfaces.ResourceManagement
{
    public interface IResourceCache : IResourceManager
    {
        // For convenience.

        void LoadLocalResources();
        void LoadBaseResources();

        /// <summary>
        ///     TEMPORARY: We need this because Godot can't load most resources without the disk easily.
        /// </summary>
        bool TryGetDiskFilePath(ResourcePath path, out string diskPath);

        T GetResource<T>(string path, bool useFallback = true)
            where T : BaseResource, new();

        T GetResource<T>(ResourcePath path, bool useFallback = true)
            where T : BaseResource, new();

        bool TryGetResource<T>(string path, out T resource)
            where T : BaseResource, new();

        bool TryGetResource<T>(ResourcePath path, out T resource)
            where T : BaseResource, new();

        void CacheResource<T>(string path, T resource)
            where T : BaseResource, new();

        void CacheResource<T>(ResourcePath path, T resource)
            where T : BaseResource, new();

        T GetFallback<T>()
            where T : BaseResource, new();
    }
}
