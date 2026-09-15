using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Systems;
using System;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization;

namespace Robust.Shared.Physics.Components;

/// <summary>
/// Gives an entity vertical velocity and collision behaviour across a z-level map network.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true, fieldDeltas: true)]
public sealed partial class ZLevelPhysicsComponent : Component
{
    /// <summary>
    /// Vertical velocity. Positive values move upward and negative values move downward.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Velocity;

    /// <summary>
    /// Fraction of vertical speed retained and reversed after an impact.
    /// Null uses the global z-level restitution CVar.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float? Bounciness;

    /// <summary>
    /// Multiplier applied to the global z-level gravity acceleration.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float GravityMultiplier = 1f;

    /// <summary>
    /// Whether this body lands on support while descending.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Fallable = true;

    /// <summary>
    /// Automatically steps the body up to high ground when moving onto it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AutoStep = true;

    /// <summary>
    /// Whether global vertical gravity changes <see cref="Velocity"/>.
    /// </summary>
    [DataField]
    public bool VelocityGravity = true;

    /// <summary>
    /// Whether <see cref="ZLevelGetVelocityEvent"/> is raised each physics step for external lift or flight forces.
    /// </summary>
    [DataField]
    public bool VelocityRaiseEvent;

    /// <summary>
    /// Vertical speed below which a grounded body can settle and sleep.
    /// Null uses the global z-level sleep-velocity CVar.
    /// </summary>
    [DataField]
    public float? SleepThreshold;

    /// <summary>
    /// Time a settled body must remain still before its z-level simulation sleeps.
    /// Null uses the global z-level sleep-time CVar.
    /// </summary>
    [DataField]
    public float? TimeToSleep;

    /// <summary>
    /// Entity that owns the selected support surface. Tile support is owned by its grid.
    /// </summary>
    [AutoNetworkedField]
    public EntityUid? SupportProvider;

    /// <summary>
    /// Kind of surface selected on <see cref="SupportProvider"/>.
    /// </summary>
    [AutoNetworkedField]
    public ZLevelSupportSurface SupportSurface;

    /// <summary>
    /// Absolute support height. Unlike render local height this is stable across z-map reparenting.
    /// </summary>
    [AutoNetworkedField]
    public float SupportHeight;

    /// <summary>
    /// Whether the body is confirmed on the selected support or moving vertically toward it.
    /// </summary>
    [AutoNetworkedField]
    public ZLevelGroundState GroundState = ZLevelGroundState.Airborne;

    internal bool Sleeping;

    internal float SleepTimer;

}

public enum ZLevelStepEvent : byte
{
    None,
    Floor,
    Ceiling,
    MapBoundaryDown,
    MapBoundaryUp,
    VelocityTurn,
    TerminalVelocity,
}

/// <summary>
/// Identifies the selected walkable surface independently from its provider entity.
/// </summary>
[Serializable, NetSerializable]
public enum ZLevelSupportSurface : byte
{
    None,
    Tile,
    HighGround,
    NetworkBoundary,
}

[Serializable, NetSerializable]
public enum ZLevelGroundState : byte
{
    Airborne,
    Grounded,
}
