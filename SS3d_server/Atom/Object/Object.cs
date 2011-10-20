using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using SS3D_shared.HelperClasses;
using Lidgren.Network;


namespace SS3D_Server.Atom.Object
{
    public class Object : Atom
    {
        public Object()
            : base()
        {

        }

         public override void Update(float framePeriod)
        {
            base.Update(framePeriod);
        }

        protected override void HandleExtendedMessage(NetIncomingMessage message)
        {
            ItemMessage i = (ItemMessage)message.ReadByte();
            switch (i)
            {
                default:
                    break;
            }
        }

        public override void Damage(int amount, int damagerId)
        {
            if(damageable)
                base.Damage(amount, damagerId);
        }

        public Object(SerializationInfo info, StreamingContext ctxt)
        {
            SerializeBasicInfo(info, ctxt);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            base.GetObjectData(info, ctxt);
        }
    }
}
