using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace SS3D.Atom.Object.Door
{
    public class Door : Object
    {
        bool open = false;
        Mogre.Vector3 slidePos;

        public Door()
            : base()
        {
            meshName = "doorMesh";
            name = "Door";
            collidable = true;
            clipping = false;
            slidePos = new Mogre.Vector3(0, -60, 0); // guess i'll just hide it under the map for now
            
        }

        protected override void HandleExtendedMessage(Lidgren.Network.NetIncomingMessage message)
        {
            open = message.ReadBoolean();
            UpdateState();
        }

        private void UpdateState()
        {
            if (open)
            {
                TranslateLocal(slidePos);
            }
            else
            {
                TranslateLocal(-slidePos);
            }
            updateRequired = true;
        }
    }
}
