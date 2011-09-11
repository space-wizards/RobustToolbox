using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D.Atom.Object.WallMounted
{
    public class FireAlarm : WallMounted
    {
        public FireAlarm()
            : base()
        {
            SetSpriteName(0, "fire_alarm_off");
            SetSpriteByIndex(0);
            collidable = false;
            snapTogrid = true;
        }
    }
}
