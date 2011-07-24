using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.HelperClasses;
using System.Runtime.Serialization;

namespace SS3d_server.Atom.Item.Tool
{
    [Serializable()]
    public class Crowbar : Tool
    {
        public Crowbar()
            : base()
        {
            name = "Crowbar";
        }

        public Crowbar(SerializationInfo info, StreamingContext ctxt)
        {
            name = (string)info.GetValue("name", typeof(string));
            position = (Vector2)info.GetValue("position", typeof(Vector2));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("name", name);
            info.AddValue("position", position);
        }

    }
}
