using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using SS3D_shared.HelperClasses;

namespace SS3d_server.Atom.Item.Tool
{
    [Serializable()]
    public class Wrench : Tool
    {
        public Wrench()
            : base()
        {
            name = "Wrench";
        }

        public Wrench(SerializationInfo info, StreamingContext ctxt)
        {
            SerializeBasicInfo(info, ctxt);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            base.GetObjectData(info, ctxt);
        }
    }
}
