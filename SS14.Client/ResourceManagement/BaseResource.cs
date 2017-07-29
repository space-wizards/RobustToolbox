using System;
using System.IO;
using SS14.Client.Resources;

namespace SS14.Client.ResourceManagement
{
    public abstract class BaseResource : IDisposable
    {
        public virtual string Fallback => null;

        public abstract void Load(ResourceCache cache, string path, Stream stream);

        public virtual void Dispose() { }
    }
}
