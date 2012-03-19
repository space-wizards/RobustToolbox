using System.Collections.Generic;
using System.Linq;
using SS13_Shared;
using SS13_Shared.GO;
using System.Drawing;
using System;
using System.Text;
using System.Reflection;
using ClientInterfaces.GOC;

namespace CGO
{
    public class PlayerActionComp : GameObjectComponent
    {
        public override ComponentFamily Family { get { return ComponentFamily.PlayerActions; } }

        public delegate void PlayerActionsChangedHandler(PlayerActionComp sender);
        public event PlayerActionsChangedHandler Changed;

        public List<PlayerAction> Actions = new List<PlayerAction>();

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
            var type = (ComponentMessageType)message.MessageParameters[0];

            switch (type)
            {
                case (ComponentMessageType.AddAction):
                    var typeName = (string)message.MessageParameters[1];
                    var uid = (uint)message.MessageParameters[2];
                    AddAction(typeName, uid);
                    break;

                case (ComponentMessageType.RemoveAction):
                    var uid2 = (uint)message.MessageParameters[1];
                    RemoveAction(uid2);
                    break;

                case (ComponentMessageType.RequestActionList):
                    UnpackFullListing(message);
                    break;

                case (ComponentMessageType.GetActionChecksum):
                    CheckFullUpdate((uint)message.MessageParameters[1]);
                    break;

                case (ComponentMessageType.CooldownAction):
                    uint uidCd = (uint)message.MessageParameters[1];
                    uint secCd = (uint)message.MessageParameters[2];
                    SetCooldown(uidCd, secCd);
                    break;

                default:
                    base.HandleNetworkMessage(message);
                    break;
            }
        }

        public void CheckActionList()
        {
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, ComponentMessageType.GetActionChecksum);
        }

        private void SetCooldown(uint uid, uint seconds)
        {
            PlayerAction toSet = Actions.FirstOrDefault(x => x.uid == uid);
            if (toSet != null)
                toSet.cooldownExpires = DateTime.Now.AddSeconds(seconds);
        }

        private void CheckFullUpdate(uint checksum) //This should never happen. This just exists in case it desynchs.
        {
            long sum = Actions.Sum(x => x.uid) * Actions.Count; //Absolutely not perfect or safe. If this causes problems later (unlikely) we can still change it.
            if (sum != checksum) Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, ComponentMessageType.RequestActionList);
        }

        public void SendDoAction(PlayerAction action, object target)
        {
            if (!Actions.Contains(action)) return;

            double cdLeft = action.cooldownExpires.Subtract(DateTime.Now).TotalSeconds;
            if (cdLeft > 0) return;

            switch (action.targetType)
            {
                case PlayerActionTargetType.Any:
                    {
                        Entity trg = (Entity)target;
                        Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, ComponentMessageType.DoAction, action.uid, action.targetType, trg.Uid);
                        break;
                    }
                case PlayerActionTargetType.None:
                    {
                        Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, ComponentMessageType.DoAction, action.uid, action.targetType, Owner.Uid);
                        break;
                    }
                case PlayerActionTargetType.Other:
                    {
                        Entity trg = (Entity)target;
                        if (trg == Owner) return;
                        Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, ComponentMessageType.DoAction, action.uid, action.targetType, trg.Uid);
                        break;
                    }
                case PlayerActionTargetType.Point:
                    {
                        PointF trg = (PointF)target;
                        Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, ComponentMessageType.DoAction, action.uid, action.targetType, trg.X, trg.Y);
                        break;
                    }
            }
        }

        private void Reset()
        {
            Actions.Clear();
            if (Changed != null) Changed(this);
        }

        private void UnpackFullListing(IncomingEntityComponentMessage message)
        {
            Reset();

            var numPacks = (uint)message.MessageParameters[1];

            for (int i = 0; i < numPacks; i++)
            {
                uint uid = (uint)message.MessageParameters[i + 2];
                string typeName = (string)message.MessageParameters[i + 3];
                AddAction(typeName, uid);
            }
        }

        public override void RecieveMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> reply, params object[] list)
        {
            base.RecieveMessage(sender, type, reply, list);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
        }

        private void AddAction(string typeName, uint uid) //Don't manually use this clientside. The server adds and removes what is needed.
        {
            Type t = Type.GetType("CGO." + typeName);
            if (t == null || !t.IsSubclassOf(typeof(PlayerAction))) return;
            PlayerAction newAction = (PlayerAction)Activator.CreateInstance(t, new object[] { uid, this });
            Actions.Add(newAction);
            if (Changed != null) Changed(this);
        }

        private void RemoveAction(uint uid) //Don't manually use this clientside. The server adds and removes what is needed.
        {
            PlayerAction toRemove = Actions.FirstOrDefault(x => x.uid == uid);
            if (toRemove != null)
            {
                Actions.Remove(toRemove);
                if (Changed != null) Changed(this);
            }
        }

        public bool HasAction(string typeName)
        {
            foreach (PlayerAction act in Actions)
                if (act.GetType().Name.Equals(typeName, StringComparison.InvariantCultureIgnoreCase))
                    return true;

            return false;
        }
    }
}
