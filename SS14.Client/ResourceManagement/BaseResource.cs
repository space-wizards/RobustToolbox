using System;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Shared.Utility;

namespace SS14.Client.ResourceManagement
{
    /// <summary>
    ///     Base resource for the cache.
    /// </summary>
    public abstract class BaseResource : IDisposable
    {
        /// <summary>
        ///     Fallback resource path if this one does not exist.
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
        /// <param name="path">Path of the resource requested on the VFS.</param>
        public abstract void Load(IResourceCache cache, ResourcePath path);
    }
}
