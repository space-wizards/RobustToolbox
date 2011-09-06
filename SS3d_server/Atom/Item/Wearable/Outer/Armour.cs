using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.HelperClasses;
using System.Runtime.Serialization;

namespace SS3D_Server.Atom.Item.Wearable.Outer
{
    [Serializable()]
    public class Armour : Outer
    {
        public Armour()
            : base()
        {
            name = "Armour";
        }

        public Armour(SerializationInfo info, StreamingContext ctxt)
        {
            SerializeBasicInfo(info, ctxt);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            base.GetObjectData(info, ctxt);
        }


    }
}
