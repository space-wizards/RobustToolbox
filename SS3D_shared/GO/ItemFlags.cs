using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D_shared.GO
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
        Mop,
        Clean
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
}
