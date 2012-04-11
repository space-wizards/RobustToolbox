using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS13_Shared.GO
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
        HealthScan
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
        Clean
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
