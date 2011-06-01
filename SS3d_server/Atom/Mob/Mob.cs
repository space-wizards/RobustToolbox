using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using SS3d_server.Atom.Mob.HelperClasses;

namespace SS3d_server.Atom.Mob
{
    public class Mob : Atom
    {
        public float walkSpeed = 1.0f;
        public float runSpeed = 2.0f;

        public Dictionary<string, HelperClasses.Appendage> appendages;
        public Appendage selectedAppendage;

        public string animationState = "idle";

        public Mob()
            : base()
        {
            initAppendages();
        }

        /// <summary>
        /// Initializes appendage dictionary
        /// </summary>
        protected virtual void initAppendages()
        {
            appendages.Add("LeftHand", new HelperClasses.Appendage("LeftHand"));
            appendages.Add("RightHand", new HelperClasses.Appendage("RightHand"));
            selectedAppendage = appendages["LeftHand"];
        }

        protected override void HandleExtendedMessage(Lidgren.Network.NetIncomingMessage message)
        {
            MobMessage mobMessageType = (MobMessage)message.ReadByte();
            switch (mobMessageType)
            {
                case MobMessage.AnimationState:
                    HandleAnimationState(message);
                    break;
                default: 
                    break;
            }
        }

        protected virtual void HandleAnimationState(NetIncomingMessage message)
        {
            string state = message.ReadString();
            NetOutgoingMessage outmessage = CreateAtomMessage();
            outmessage.Write((byte)AtomMessage.Extended);
            outmessage.Write((byte)MobMessage.AnimationState);
            outmessage.Write(state);
            SendMessageToAll(outmessage);
        }

        /// <summary>
        /// Selects an appendage from the dictionary
        /// </summary>
        /// <param name="appendageName">name of appendage to select</param>
        public virtual void SelectAppendage(string appendageName)
        {
            if (appendages.Keys.Contains(appendageName))
                selectedAppendage = appendages[appendageName];
            SendSelectAppendage();
        }

        /// <summary>
        /// Sends a message to all clients telling them the mob has selected an appendage.
        /// </summary>
        public virtual void SendSelectAppendage()
        {
            NetOutgoingMessage outmessage = CreateAtomMessage();
            outmessage.Write((byte)AtomMessage.Extended);
            outmessage.Write((byte)MobMessage.SelectAppendage);
            outmessage.Write(selectedAppendage.appendageName);
            SendMessageToAll(outmessage);
        }
    }
}
