using System;
using System.Threading;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.Client.ResourceManagement;

/// <summary>
///     Base resource for the cache.
/// </summary>
public abstract class BaseResource : IDisposable
{
    /// <summary>
    ///     Fallback resource path if this one does not exist.
    /// </summary>
    public virtual ResPath? Fallback => null;

    /// <summary>
    ///     Disposes this resource.
    /// </summary>
    public virtual void Dispose()
    {
    }

    /// <summary>
    ///     Deserializes the resource from the VFS.
    /// </summary>
    public abstract void Load(IDependencyCollection dependencies, ResPath path);

    public virtual void Reload(IDependencyCollection dependencies, ResPath path, CancellationToken ct = default)
    {

    }
}
