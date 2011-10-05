using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D_shared.GO
{
    [Flags]
    public enum ItemFlag
    {
        MeleeWeapon = 1,
        Tool = 2,
        Gun = 4,
        Medical = 8,
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

    public enum MeleeType
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
