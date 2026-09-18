using Robust.Shared.Configuration;

namespace Robust.Shared;

public abstract partial class CVars
{
    /// <summary>
    /// Half-life for vertical prediction corrections.
    /// </summary>
    public static readonly CVarDef<float> NetInterpZCorrectionHalfLife =
        CVarDef.Create("net.interp_z_correction_half_life", 0f, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    /// Number of z-level maps below each viewer to include in that client's PVS.
    /// </summary>
    public static readonly CVarDef<int> NetPvsZLevelsBelow =
        CVarDef.Create("net.pvs_zlevels_below", 0, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    /// Number of z-level maps above each viewer to include in that client's PVS.
    /// </summary>
    public static readonly CVarDef<int> NetPvsZLevelsAbove =
        CVarDef.Create("net.pvs_zlevels_above", 0, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    /// Virtual distance between adjacent z-levels for positional audio attenuation.
    /// </summary>
    public static readonly CVarDef<float> AudioZLevelDistance =
        CVarDef.Create("audio.z_level_distance", 0f, CVar.REPLICATED);

    /// <summary>
    /// Whether explicitly linked z-level grids receive cross-map soft velocity corrections.
    /// </summary>
    public static readonly CVarDef<bool> ZLevelGridSync =
        CVarDef.Create("zlevel.grid_sync", false, CVar.REPLICATED);

    /// <summary>
    /// Fixed update frequency for continuous z-axis physics.
    /// </summary>
    public static readonly CVarDef<int> ZLevelTickRate =
        CVarDef.Create("zlevel.tick_rate", 0, CVar.REPLICATED);

    /// <summary>
    /// Acceleration applied to z-level physics bodies with vertical gravity enabled.
    /// </summary>
    public static readonly CVarDef<float> ZLevelGravity =
        CVarDef.Create("zlevel.gravity", 0f, CVar.REPLICATED);

    /// <summary>
    /// Absolute terminal velocity for z-level physics bodies.
    /// </summary>
    public static readonly CVarDef<float> ZLevelVelocityLimit =
        CVarDef.Create("zlevel.velocity_limit", 0f, CVar.REPLICATED);

    /// <summary>
    /// Minimum vertical speed that raises a damage-capable impact event.
    /// </summary>
    public static readonly CVarDef<float> ZLevelImpactVelocity =
        CVarDef.Create("zlevel.impact_velocity", 0f, CVar.REPLICATED);

    /// <summary>
    /// Distance above support at which a z-level body is considered airborne.
    /// </summary>
    public static readonly CVarDef<float> ZLevelAirborneHeight =
        CVarDef.Create("zlevel.airborne_height", 0f, CVar.REPLICATED);

    /// <summary>
    /// Largest support rise that a grounded z-level body may step onto.
    /// </summary>
    public static readonly CVarDef<float> ZLevelMaxStepUp =
        CVarDef.Create("zlevel.max_step_up", 0f, CVar.REPLICATED);

    /// <summary>
    /// Largest support drop that remains a grounded step rather than beginning a fall.
    /// </summary>
    public static readonly CVarDef<float> ZLevelMaxStepDown =
        CVarDef.Create("zlevel.max_step_down", 0f, CVar.REPLICATED);

    /// <summary>
    /// Maximum downward distance over which a grounded body snaps to support.
    /// </summary>
    public static readonly CVarDef<float> ZLevelGroundSnapDistance =
        CVarDef.Create("zlevel.ground_snap_distance", 0f, CVar.REPLICATED);

    /// <summary>
    /// Default vertical speed retained after an impact.
    /// </summary>
    public static readonly CVarDef<float> ZLevelRestitution =
        CVarDef.Create("zlevel.restitution", 0f, CVar.REPLICATED);

    /// <summary>
    /// Vertical speed below which a supported z-level body can settle.
    /// </summary>
    public static readonly CVarDef<float> ZLevelSleepVelocityThreshold =
        CVarDef.Create("zlevel.sleep_velocity_threshold", 0f, CVar.REPLICATED);

    /// <summary>
    /// Time a settled z-level body remains still before its vertical simulation sleeps.
    /// </summary>
    public static readonly CVarDef<float> ZLevelSleepTime =
        CVarDef.Create("zlevel.sleep_time", 0f, CVar.REPLICATED);

    /// <summary>
    /// Vertical distance from support that still counts as settled for sleeping.
    /// </summary>
    public static readonly CVarDef<float> ZLevelGroundTolerance =
        CVarDef.Create("zlevel.ground_tolerance", 0f, CVar.REPLICATED);

    /// <summary>
    /// Radius around a body's support point used to retain stable support near surface edges.
    /// </summary>
    public static readonly CVarDef<float> ZLevelSupportBuffer =
        CVarDef.Create("zlevel.support_buffer", 0f, CVar.REPLICATED);

    /// <summary>
    /// Collision footprint radius used by z transitions for bodies without hard fixtures.
    /// </summary>
    public static readonly CVarDef<float> ZLevelFallbackFootprintRadius =
        CVarDef.Create("zlevel.fallback_footprint_radius", 0f, CVar.REPLICATED);

    /// <summary>
    /// Radius around a changed high-ground provider in which body support is refreshed.
    /// </summary>
    public static readonly CVarDef<float> ZLevelSupportRefreshRange =
        CVarDef.Create("zlevel.support_refresh_range", 0f, CVar.REPLICATED);
}
