using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.HelperClasses;
using System.Runtime.Serialization;
using External = SGO.Component.Health.Organs.External;

namespace SGO.Component.Health.Organs.Internal
{
    public class Internal : Organ 
    {
        public External.External holder; // The organ we are currently in (if any)
                
        public Internal()
            : base()
        { 
        }

        public override void SetUp(HealthComponent _owner)
        {
            base.SetUp(_owner);
        }

        public override void ConnectOrgan()
        {
            if (owner == null)
                return;
            foreach (Organs.External.External O in owner.organs)
            {
                if (O.GetType() == masterConnectionType)
                {
                    masterConnection = O;
                    O.internalChildren.Add(this);
                    break;
                }
            }
        }

        public override void Process(float frametime)
        {
            base.Process(frametime);
        }

    }
}
