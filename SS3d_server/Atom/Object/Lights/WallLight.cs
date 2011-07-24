using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using SS3D_shared.HelperClasses;
using System.Runtime.Serialization;

namespace SS3d_server.Atom.Object.Lights
{
    [Serializable()]
    public class WallLight : Object
    {

        public Item.Light light;
        public WallLight()
            : base()
        {
            name = "WallLight";
            light = new Item.Light(new Item.Color(200, 200, 200), Direction.South);
            light.Normalize();
        }

        public override void SerializedInit()
        {
            base.SerializedInit();
            light = new Item.Light(new Item.Color(200, 200, 200), Direction.South);
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

        public WallLight(SerializationInfo info, StreamingContext ctxt)
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
