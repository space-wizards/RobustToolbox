using Robust.Shared.Utility;

namespace Robust.Client.ResourceManagement;

/// <summary>
/// Defines type-level metadata for resource types.
/// </summary>
public interface IBaseResource
{
    /// <summary>
    /// Whether resources of this type can be deterministically removed from the cache.
    /// </summary>
    static virtual bool CanBeRemoved => false;

    /// <summary>
    /// The fallback path for this resource type (if any).
    /// </summary>
    static virtual ResPath? FallbackPath => null;
}
