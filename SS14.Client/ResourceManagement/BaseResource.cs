using System;
using System.IO;
using SS14.Client.Resources;

namespace SS14.Client.ResourceManagement
{
    /// <summary>
    ///     Base resource for the cache.
    /// </summary>
    public abstract class BaseResource : IDisposable
    {
        /// <summary>
        ///     Fallback resource if this one does not exist.
        /// </summary>
        public virtual string Fallback => null;

        /// <summary>
        ///     Disposes this resource.
        /// </summary>
        public virtual void Dispose()
        {
        }

        /// <summary>
        ///     Deserializes the resource from the VFS.
        /// </summary>
        /// <param name="cache">ResourceCache this resource is being loaded into.</param>
        /// <param name="path">Path of the resource relative to the root of the ResourceCache.</param>
        /// <param name="stream">Stream of the resource that was opened.</param>
        public abstract void Load(ResourceCache cache, string path, Stream stream);
    }
}
