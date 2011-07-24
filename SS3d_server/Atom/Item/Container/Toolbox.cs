using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using SS3D_shared.HelperClasses;

namespace SS3d_server.Atom.Item.Container
{
    [Serializable()]
    public class Toolbox : Item
    {
        public Toolbox()
            : base()
        {
            name = "Toolbox";
        }

        public Toolbox(SerializationInfo info, StreamingContext ctxt)
        {
            SerializeBasicInfo(info, ctxt);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            base.GetObjectData(info, ctxt);
        }
    }
}
