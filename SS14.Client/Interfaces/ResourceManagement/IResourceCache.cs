using Godot;
using SS14.Client.ResourceManagement;
using SS14.Shared.GameObjects;
using System.Collections.Generic;
using System.IO;
using SS14.Shared.Interfaces;

namespace SS14.Client.Interfaces.ResourceManagement
{
    public interface IResourceCache
    {
        // For convenience.

        void LoadLocalResources();
        void LoadBaseResources();

        T GetResource<T>(string path, bool useFallback=true)
            where T : BaseResource, new();

        bool TryGetResource<T>(string path, out T resource)
            where T : BaseResource, new();

        void CacheResource<T>(string path, T resource)
            where T : BaseResource, new();
    }
}
