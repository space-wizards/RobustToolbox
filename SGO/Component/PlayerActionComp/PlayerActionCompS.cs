using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;
using ServerInterfaces;
using ServerInterfaces.GameObject;
using ServerInterfaces.Player;

namespace SGO
{
    public class PlayerActionComp : GameObjectComponent
    {
        public List<PlayerAction> Actions = new List<PlayerAction>();
        private uint uidCurr;

        public PlayerActionComp()
        {
            family = ComponentFamily.PlayerActions;
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection client)
        {
            var type = (ComponentMessageType) message.MessageParameters[0];

            switch (type)
            {
                case (ComponentMessageType.RequestActionList):
                    SendFullListing(client);
                    break;

                case (ComponentMessageType.GetActionChecksum):
                    Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, client,
                                                      ComponentMessageType.GetActionChecksum,
                                                      (uint) (Actions.Sum(x => x.uid)*Actions.Count));
                    break;

                case (ComponentMessageType.DoAction):
                    DoAction(message);
                    break;

                default:
                    base.HandleNetworkMessage(message, client);
                    break;
            }
        }

        private void DoAction(IncomingEntityComponentMessage message)
        {
            PlayerAction toDo =
                Actions.FirstOrDefault(
                    x =>
                    x.uid == (uint) message.MessageParameters[1] &&
                    (PlayerActionTargetType) message.MessageParameters[2] == x.targetType);
            if (toDo != null)
            {
                double cdLeft = toDo.cooldownExpires.Subtract(DateTime.Now).TotalSeconds;
                if (cdLeft > 0) //Tried to use something while on cooldown. Shouldnt happen but might.
                {
                    return; //Send fail later?
                }
                else
                {
                    if (toDo.targetType == PlayerActionTargetType.Any || toDo.targetType == PlayerActionTargetType.Other ||
                        toDo.targetType == PlayerActionTargetType.None)
                        //Check validity of targets later. Only clientside atm.
                    {
                        IEntity ent = EntityManager.Singleton.GetEntity((int) message.MessageParameters[3]);
                        //ent id clienside uint but server int. Why?!
                        if (ent != null)
                        {
                            toDo.OnUse(ent);
                        }
                        else
                            return; //Invalid target. Send fail later?
                    }
                    else if (toDo.targetType == PlayerActionTargetType.Point)
                    {
                        var trg = new PointF((float) message.MessageParameters[3], (float) message.MessageParameters[4]);
                        toDo.OnUse(trg);
                    }
                }
            }
            else
            {
                //They asked us to do something we don't have.
                //Send full listing or error ?! TODO.
            }
        }

        public void StartCooldown(PlayerAction act)
        {
            if (act.cooldownSeconds == 0) return;
            act.cooldownExpires = DateTime.Now.AddSeconds(act.cooldownSeconds);
            if (GetMyOwnerConnection() != null)
                Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, GetMyOwnerConnection(),
                                                  ComponentMessageType.CooldownAction, act.uid, act.cooldownSeconds);
        }

        public override void HandleInstantiationMessage(NetConnection netConnection)
        {
            base.HandleInstantiationMessage(netConnection);

            if (GetMyOwnerConnection() != null && GetMyOwnerConnection() == netConnection)
            {
                AddAction("ExampleAction");
                //This is a terrible place for this and only exists for testing. REMOVE THIS.
                SendFullListing(GetMyOwnerConnection());
            }
        }

        private void SendFullListing(NetConnection client)
        {
            var message = new object[(Actions.Count*2) + 2];
            message[0] = ComponentMessageType.RequestActionList;
            message[1] = (uint) Actions.Count;

            uint index = 2;
            foreach (PlayerAction act in Actions)
            {
                message[index] = act.uid;
                message[index + 1] = act.GetType().Name;
                index += 2;
            }

            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, client, message);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
        }

        private NetConnection GetMyOwnerConnection()
        {
            var replies = new List<ComponentReplyMessage>();
            Owner.SendMessage(this, ComponentMessageType.GetActorSession, replies);

            if (replies.Count > 0 && replies[0].MessageType == ComponentMessageType.ReturnActorSession)
            {
                var session = (IPlayerSession) replies[0].ParamsList[0];
                return session.ConnectedClient;
            }
            else return null;
        }

        public uint? AddAction(string typeName)
        {
            Type t = Type.GetType("SGO." + typeName);
            if (t == null || !t.IsSubclassOf(typeof (PlayerAction))) return null;

            uint nextUid = uidCurr++; //Increases uid even if adding fails due to effect being unique. fix.

            var newAction = (PlayerAction) Activator.CreateInstance(t, new object[] {nextUid, this});

            Actions.Add(newAction);

            if (GetMyOwnerConnection() != null)
                Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, GetMyOwnerConnection(),
                                                  ComponentMessageType.AddAction, typeName, nextUid);

            return nextUid;
        }

        public void RemoveAction(uint uid)
        {
            PlayerAction toRemove = Actions.FirstOrDefault(x => x.uid == uid);
            if (toRemove != null)
            {
                Actions.Remove(toRemove);

                if (GetMyOwnerConnection() != null)
                    Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, GetMyOwnerConnection(),
                                                      ComponentMessageType.RemoveAction, toRemove.uid);
            }
        }

        public void RemoveAction(string typeName)
        {
            PlayerAction toRemove =
                Actions.FirstOrDefault(
                    x => x.GetType().Name.Equals(typeName, StringComparison.InvariantCultureIgnoreCase));
            if (toRemove != null)
            {
                Actions.Remove(toRemove);

                if (GetMyOwnerConnection() != null)
                    Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, GetMyOwnerConnection(),
                                                      ComponentMessageType.RemoveAction, toRemove.uid);
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