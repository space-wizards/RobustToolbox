using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Spawners;

/// <summary>
/// Put this component on something you would like to despawn after a certain amount of time
/// </summary>
/// <remarks>
/// NOT networked as we don't want clients predicting networked entity deletions.
/// </remarks>
[RegisterComponent]
public sealed partial class TimedDespawnComponent : Component
{
    /// <summary>
    /// How long the entity will exist before despawning
    /// </summary>
    [DataField("lifetime")]
    public float Lifetime = 5f;
}
