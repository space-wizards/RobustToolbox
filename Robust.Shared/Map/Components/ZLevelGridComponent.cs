using System.Numerics;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Map.Components;

/// <summary>
/// Explicit association between grids that occupy corresponding space on adjacent z-level maps.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, fieldDeltas: true), UnsavedComponent]
[Access(typeof(ZLevelSystem))]
public sealed partial class ZLevelGridComponent : Component
{
    [AutoNetworkedField]
    public EntityUid? GridAbove;

    [AutoNetworkedField]
    public EntityUid? GridBelow;

    [DataField, AutoNetworkedField]
    public bool SyncLinkedGrids = true;

    [DataField, AutoNetworkedField]
    public Vector2 GridAboveOffset;

    [DataField, AutoNetworkedField]
    public Angle GridAboveRotation = Angle.Zero;

    [DataField, AutoNetworkedField]
    public Vector2 GridBelowOffset;

    [DataField, AutoNetworkedField]
    public Angle GridBelowRotation = Angle.Zero;

    [DataField, AutoNetworkedField]
    public float SyncLinearFrequency = 8f;

    [DataField, AutoNetworkedField]
    public float SyncLinearDamping = 1.2f;

    [DataField, AutoNetworkedField]
    public float SyncAngularFrequency = 8f;

    [DataField, AutoNetworkedField]
    public float SyncAngularDamping = 1.2f;

    [DataField, AutoNetworkedField]
    public float SyncMaxLinearCorrection = 20f;

    [DataField, AutoNetworkedField]
    public float SyncMaxAngularCorrection = 12f;

    [DataField, AutoNetworkedField]
    public float SyncSnapDistance = 8f;

    [DataField, AutoNetworkedField]
    public Angle SyncSnapRotation = Angle.FromDegrees(45);
}
