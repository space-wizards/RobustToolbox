using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using SS3D_shared.GO;

namespace SS3D_Server.Atom.Object
{
    [Serializable()]
    class Locker : Object
    {
        private bool open = false;
        public Locker()
            : base()
        {

        }

        public Locker(SerializationInfo info, StreamingContext ctxt)
        {
            SerializeBasicInfo(info, ctxt);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            base.GetObjectData(info, ctxt);
        }

        
        protected override void ApplyAction(Atom a, Mob.Mob m)
        {
            /*base.ApplyAction(a, m);

            open = !open;
            if (open)
                SendMessage(null, ComponentMessageType.SetSpriteByKey, null, "locker_open");
            //SetSpriteState(1);
            else
                SendMessage(null, ComponentMessageType.SetSpriteByKey, null, "locker_closed");
                //SetSpriteState(0);*/
        }
    }
}
