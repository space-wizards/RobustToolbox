using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CGO;

namespace SS3D.Atom.Object.WallMounted
{
    public class FireAlarm : WallMounted
    {
        public FireAlarm()
            : base()
        {
            ISpriteComponent c = (ISpriteComponent)GetComponent(SS3D_shared.GO.ComponentFamily.Renderable);
            c.AddSprite("fire_alarm_off");
            c.SetSpriteByKey("fire_alarm_off");
            collidable = false;
            snapTogrid = true;
        }
    }
}
