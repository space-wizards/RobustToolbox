using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;

namespace SS3d_server.Atom.Mob
{
    public class Mob : Atom
    {
        public float walkSpeed = 1.0f;
        public float runSpeed = 2.0f;

        public Atom leftHandItem; // Just a temporary storage spot, for now.
        public Atom rightHandItem;
        public MobHand selectedHand = MobHand.LHand; // The hand we are currently using, and also used to tell clients which
        // hand to attach items to on remote mobs when the pick something up.

        public string animationState = "idle";

        public Mob()
            : base()
        {

        }

        protected override void HandleExtendedMessage(Lidgren.Network.NetIncomingMessage message)
        {
            base.HandleExtendedMessage(message);
            switch ((MobMessage)message.ReadByte())
            {
                case MobMessage.AnimationState:
                    HandleAnimationState(message);
                    break;
                default: break;
            }
        }

        protected virtual void HandleAnimationState(NetIncomingMessage message)
        {
            string state = message.ReadString();
            NetOutgoingMessage outmessage = CreateAtomMessage();
            outmessage.Write((byte)MobMessage.AnimationState);
            outmessage.Write(state);
            SendMessageToAll(outmessage);
        }
    }
}
