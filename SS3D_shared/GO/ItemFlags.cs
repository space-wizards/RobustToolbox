using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D_shared.GO
{
    [Flags]
    public enum ItemTypeFlag
    {
        MeleeWeapon = 1,
        Tool = 2,
        Gun = 4,
        Medical = 8,
    }

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
        Medical
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
    }

    public enum ToolAction
    {
        Weld,
        Wrench,
        Pry,
        Screw,
        Hit,
        Cut,
    }

    public enum DamageType
    {
        Hit,
        Slice,
        Pierce,
        Bludgeon,
        Trip
    }

    public enum GunType
    {
        Bullet,
        Beam,
        EnergyBall,
        Flame
    }

    public enum HealAction
    {
        Diagnose,
        FixBruise,
        FixCut,
        FixBurn,
    }
}
