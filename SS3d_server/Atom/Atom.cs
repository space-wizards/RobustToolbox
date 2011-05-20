using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.HelperClasses;

namespace SS3d_server.Atom
{
    public class Atom // SERVER SIDE
    {
        public string name;
        public ushort UID
        {
            private set;
            get;
        }

        // Position data
        public Vector3 position;
        public float rotW;
        public float rotY;

        public List<InterpolationPacket> interpolationPacket;

        public void Update()
        {
            //Updates the atom, item, whatever. This should be called from the atom manager's update queue.
        }

    }
}
