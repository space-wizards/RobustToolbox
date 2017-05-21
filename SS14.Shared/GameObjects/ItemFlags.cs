using System;

namespace SS14.Shared.GameObjects
{
    [Flags]
    public enum InteractsWith
    {
        LargeObject,
        Actor,
        Item
    }

    public enum ItemCapabilityType
    {
        None,
        MeleeWeapon,
        Tool,
        Gun,
        Medical,
        HealthScan,
        Internals
    }

    public enum ItemCapabilityVerb
    {
        Weld,
        Wrench,
        Pry,
        Screw,
        Hit,
        Cut,
        Slice,
        Pierce,
        Bludgeon,
        Trip,
        Diagnose,
        FixBruise,
        FixCut,
        FixBurn,
        Mop,
        Clean,
        Emag
    }

    public enum DamageType
    {
        Piercing,
        Bludgeoning,
        Slashing,
        Toxin,
        Burn,
        Freeze,
        Suffocation,
        Shock,
        BloodLoss,
        Collateral,
        Untyped
    }

    public enum GunType
    {
        Bullet,
        Beam,
        EnergyBall,
        Flame
    }
}
