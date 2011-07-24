using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using SS3D_shared.HelperClasses;
using System.Runtime.Serialization;

namespace SS3d_server.Atom.Item.Misc
{
    [Serializable()]
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
            light = new Light(new Color((byte)r.Next(255), (byte)r.Next(255), (byte)r.Next(255)), (Direction)r.Next(3));
            light.Normalize();
        }

        public override void SerializedInit()
        {
            base.SerializedInit();
            Random r = new Random(DateTime.Now.Millisecond);
            int milli = DateTime.Now.Millisecond;
            while (milli == DateTime.Now.Millisecond)
            {
            }
            light = new Light(new Color((byte)r.Next(255), (byte)r.Next(255), (byte)r.Next(255)), (Direction)r.Next(3));
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

        public override void SendState()
        {
            base.SendState();

            NetOutgoingMessage msg = CreateAtomMessage();
            msg.Write((byte)AtomMessage.Push);
            msg.Write(light.color.r);
            msg.Write(light.color.g);
            msg.Write(light.color.b);
            msg.Write((byte)light.direction);
            SendMessageToAll(msg);
        }



        public Flashlight(SerializationInfo info, StreamingContext ctxt)
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
