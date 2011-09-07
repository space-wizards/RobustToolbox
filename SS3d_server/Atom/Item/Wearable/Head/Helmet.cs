using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.HelperClasses;
using System.Runtime.Serialization;

namespace SS3D_Server.Atom.Item.Wearable.Head
{
    [Serializable()]
    public class Helmet : Head
    {
        public Helmet()
            : base()
        {
            name = "Helmet";
        }

        public Helmet(SerializationInfo info, StreamingContext ctxt)
        {
            SerializeBasicInfo(info, ctxt);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            base.GetObjectData(info, ctxt);
        }


    }
}
