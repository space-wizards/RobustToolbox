using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;

namespace SS3d_server.Atom.Item.Misc
{
    public class Flashlight : Item
    {

        public Light light;

        public Flashlight()
            : base()
        {
            name = "Flashlight";
            Random r = new Random(DateTime.Now.Millisecond);
            int milli = DateTime.Now.Millisecond;
            while (milli == DateTime.Now.Millisecond)
            {
            }
            light = new Light(new Color((byte)r.Next(255), (byte)r.Next(255), (byte)r.Next(255)), (LightDirection)r.Next(3));
            light.Normalize();
        }

        public override void SendState(Lidgren.Network.NetConnection client)
        {
            base.SendState(client);

            NetOutgoingMessage msg = CreateAtomMessage();
            msg.Write((byte)AtomMessage.Push);
            msg.Write(light.color.r);
            msg.Write(light.color.g);
            msg.Write(light.color.b);
            msg.Write((byte)light.direction);
            SendMessageTo(msg, client);
        }
    }
}
