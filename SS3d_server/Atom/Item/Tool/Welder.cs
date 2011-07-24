using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using SS3D_shared.HelperClasses;

namespace SS3d_server.Atom.Item.Tool
{
    [Serializable()]
    public class Welder : Tool
    {
        public Welder()
            : base()
        {
            name = "Welder";
        }

        public Welder(SerializationInfo info, StreamingContext ctxt)
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
