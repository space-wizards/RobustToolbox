using SS14.Client.Interfaces.ResourceManagement;
using SS14.Shared.Interfaces;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Client.ResourceManagement
{
    public class ResourceCache : IResourceCache
    {
        [Dependency]
        readonly IConfigurationManager _config;

        [Dependency]
        readonly IResourceManager _resources;

        public void LoadBaseResources()
        {
            _resources.Initialize();

            _resources.MountContentDirectory(@"./Resources/");
            _resources.MountContentPack(@"./EngineContentPack.zip");
        }

        public void LoadLocalResources()
        {
            _resources.MountDefaultContentPack();
        }

        public T GetResource<T>(string path) where T : BaseResource, new()
        {
            throw new NotImplementedException();
        }

        public bool TryGetResource<T>(string path, out T resource) where T : BaseResource, new()
        {
            throw new NotImplementedException();
        }

        public void CacheResource<T>(string path, T resource) where T : BaseResource, new()
        {
            throw new NotImplementedException();
        }
    }
}
