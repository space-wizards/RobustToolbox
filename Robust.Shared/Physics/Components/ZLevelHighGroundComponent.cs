using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Physics.Components;

/// <summary>
/// Defines a flat walkable surface on an entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(ZLevelPhysicsSystem), typeof(ZLevelSupportSystem))]
public sealed partial class ZLevelHighGroundComponent : Component
{
    /// <summary>
    /// Flat walkable surface height above the provider's map plane.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Height = 1.05f;

    /// <summary>
    /// Whether fixtures beneath the walkable surface represent a solid vertical volume. Walls use this; flat platforms usually do not.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool SolidVolume = true;

    /// <summary>
    /// Fixture whose shape defines the walkable surface.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public string SurfaceFixture;
}
