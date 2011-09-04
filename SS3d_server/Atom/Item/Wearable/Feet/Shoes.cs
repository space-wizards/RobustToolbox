using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.HelperClasses;
using System.Runtime.Serialization;

namespace SS3D_Server.Atom.Item.Wearable.Feet
{
    public class Shoes : Feet
    {
        public Shoes()
            : base()
        {
            name = "Shoes";
        }

        public Shoes(SerializationInfo info, StreamingContext ctxt)
        {
            SerializeBasicInfo(info, ctxt);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            base.GetObjectData(info, ctxt);
        }


    }
}
