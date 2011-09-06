using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using SS3D_shared.HelperClasses;

namespace SS3D_Server.Atom.Item.Tool
{
    [Serializable()]
    public class Hat : Tool
    {
        public Hat()
            : base()
        {
            name = "Hat";
        }

        public Hat(SerializationInfo info, StreamingContext ctxt)
        {
            SerializeBasicInfo(info, ctxt);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            base.GetObjectData(info, ctxt);
        }
    }
}
