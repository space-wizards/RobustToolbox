using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace SS3D_Server.Atom.Object.WallMounted
{
    [Serializable()]
    public class Extinguisher : WallMounted
    {
        public Extinguisher()
            : base()
        {
            name = "Extinguisher";
        }

        public Extinguisher(SerializationInfo info, StreamingContext ctxt)
        {
            SerializeBasicInfo(info, ctxt);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            base.GetObjectData(info, ctxt);
        }
    }
}
