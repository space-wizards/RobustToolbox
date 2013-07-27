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
        public PlayerActionComp() : base()
        {
            Family = ComponentFamily.PlayerActions;
        }
        public delegate void PlayerActionsChangedHandler(PlayerActionComp sender);
        public event PlayerActionsChangedHandler Changed;

        public List<IPlayerAction> Actions = new List<IPlayerAction>();

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
            IPlayerAction toSet = Actions.FirstOrDefault(x => x.Uid == uid);
            if (toSet != null)
                toSet.CooldownExpires = DateTime.Now.AddSeconds(seconds);
        }

        private void CheckFullUpdate(uint checksum) //This should never happen. This just exists in case it desynchs.
        {
            long sum = Actions.Sum(x => x.Uid) * Actions.Count; //Absolutely not perfect or safe. If this causes problems later (unlikely) we can still change it.
            if (sum != checksum) Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, ComponentMessageType.RequestActionList);
        }

        public void SendDoAction(IPlayerAction action, object target)
        {
            if (!Actions.Contains(action)) return;

            double cdLeft = action.CooldownExpires.Subtract(DateTime.Now).TotalSeconds;
            if (cdLeft > 0) return;

            switch (action.TargetType)
            {
                case PlayerActionTargetType.Any:
                    {
                        Entity trg = (Entity)target;
                        Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, ComponentMessageType.DoAction, action.Uid, action.TargetType, trg.Uid);
                        break;
                    }
                case PlayerActionTargetType.None:
                    {
                        Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, ComponentMessageType.DoAction, action.Uid, action.TargetType, Owner.Uid);
                        break;
                    }
                case PlayerActionTargetType.Other:
                    {
                        Entity trg = (Entity)target;
                        if (trg == Owner) return;
                        Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, ComponentMessageType.DoAction, action.Uid, action.TargetType, trg.Uid);
                        break;
                    }
                case PlayerActionTargetType.Point:
                    {
                        PointF trg = (PointF)target;
                        Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, ComponentMessageType.DoAction, action.Uid, action.TargetType, trg.X, trg.Y);
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
                uint uid = (uint)message.MessageParameters[2 + (i * 2)];
                string typeName = (string)message.MessageParameters[3 + (i * 2)];
                AddAction(typeName, uid);
            }
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
            IPlayerAction toRemove = Actions.FirstOrDefault(x => x.Uid == uid);
            if (toRemove != null)
            {
                Actions.Remove(toRemove);
                if (Changed != null) Changed(this);
            }
        }

        public bool HasAction(string typeName)
        {
            foreach (IPlayerAction act in Actions)
                if (act.GetType().Name.Equals(typeName, StringComparison.InvariantCultureIgnoreCase))
                    return true;

            return false;
        }
    }
}
