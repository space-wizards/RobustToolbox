using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace SS3D.Atom.Object.Door
{
    public class Door : Object
    {
        DoorState status = DoorState.Closed;
        DoorState laststatus = DoorState.Closed;

        public Door()
            : base()
        {
            spritename = "Door";
            collidable = true;
        }

        protected override void HandleExtendedMessage(Lidgren.Network.NetIncomingMessage message)
        {
            laststatus = status;
            status = (DoorState)message.ReadByte();
            UpdateStatus();
        }

        // This is virtual so future doors can override it if they
        // want different things to happen depending on their status
        public virtual void UpdateStatus()
        {
            switch (status)
            {
                case DoorState.Closed:
                    visible = true;
                    collidable = true;
                    break;
                case DoorState.Open:
                    visible = false;
                    collidable = false;
                    break;
                case DoorState.Broken:
                    break;
                default:
                    break;
            }
            atomManager.gameState.map.GetTileAt(position).sightBlocked = visible;
            atomManager.gameState.map.needVisUpdate = true;
            updateRequired = true;
        }

        public override void Update(double time)
        {
            base.Update(time);
            if (this.interpolationPackets.Count == 0)
            {
                UpdateStatus();
            }
            else
            {
                updateRequired = true;
            }


        }
    }
}
