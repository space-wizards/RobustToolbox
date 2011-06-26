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
        Mogre.Vector3 slidePos;
        private int slideStepsTotal = 10;
        private int slideStepsCurrent = 10;

        public Door()
            : base()
        {
            meshName = "doorMesh";
            name = "Door";
            collidable = true;
            clipping = false;
            slidePos = new Mogre.Vector3(0, 40, 0); // guess i'll just hide it under the map for now
        }

        protected override void HandleExtendedMessage(Lidgren.Network.NetIncomingMessage message)
        {
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
                    slideStepsCurrent = 0;
                    break;
                case DoorState.Open:
                    slideStepsCurrent = 0;
                    break;
                case DoorState.Broken:
                    slideStepsCurrent = 0;
                    break;
                default:
                    break;
            }
            updateRequired = true;
        }

        public override void Update()
        {
            base.Update();
            if (slideStepsCurrent < slideStepsTotal)
            {
                switch (status)
                {
                    case DoorState.Closed:
                        TranslateLocal(-slidePos / slideStepsTotal);
                        break;
                    case DoorState.Open:
                        TranslateLocal(slidePos / slideStepsTotal);
                        break;
                    case DoorState.Broken:
                        TranslateLocal(slidePos / 2);
                        slideStepsCurrent = slideStepsTotal;
                        break;
                }
                slideStepsCurrent++;
                updateRequired = true;
            }
            
        }
    }
}
