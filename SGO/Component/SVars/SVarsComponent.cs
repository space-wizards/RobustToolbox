using System.Collections.Generic;
using GameObject;
using Lidgren.Network;
using SS13.IoC;
using SS13_Shared;
using SS13_Shared.GO;
using SS13_Shared.ServerEnums;
using ServerInterfaces.Player;
using ServerServices.Log;

namespace SGO
{
    public class SVarsComponent : Component
    {
        public SVarsComponent()
        {
            Family = ComponentFamily.SVars;
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection sender)
        {
            switch ((ComponentMessageType) message.MessageParameters[0])
            {
                case ComponentMessageType.GetSVars:
                    HandleGetSVars(sender);
                    break;
                case ComponentMessageType.SetSVar:
                    HandleSetSVar((byte[]) message.MessageParameters[1], sender);
                    break;
            }
        }

        /// <summary>
        /// This is all kinds of fucked, but basically it marshals an SVar from the client and poops
        /// it forward to the component named in the message.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="client"></param>
        internal void HandleSetSVar(byte[] serializedParameter, NetConnection client)
        {
            MarshalComponentParameter parameter = MarshalComponentParameter.Deserialize(serializedParameter);
            //Check admin status -- only admins can get svars.
            IPlayerSession player = IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(client);
            if (!player.adminPermissions.isAdmin)
            {
                LogManager.Log("Player " + player.name + " tried to set an SVar, but is not an admin!", LogLevel.Warning);
            }
            else
            {
                Owner.GetComponent<Component>(parameter.Family).SetSVar(parameter);
                LogManager.Log("Player " + player.name + " set SVar."); //Make this message better
            }
        }

        /// <summary>
        /// Sends all available SVars to the client that requested them.
        /// </summary>
        /// <param name="client"></param>
        internal void SendSVars(NetConnection client)
        {
            var svars = new List<MarshalComponentParameter>();
            var serializedSvars = new List<byte[]>();
            foreach (Component component in Owner.GetComponents())
            {
                svars.AddRange(component.GetSVars());
            }

            foreach (MarshalComponentParameter svar in svars)
            {
                serializedSvars.Add(svar.Serialize());
            }
            var parameters = new List<object>();
            parameters.Add(ComponentMessageType.GetSVars);
            parameters.Add(serializedSvars.Count);
            parameters.AddRange(serializedSvars);
            Owner.SendDirectedComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, client,
                                                      parameters.ToArray());
        }

        /// <summary>
        /// Handle a getSVars message
        /// checks for admin access
        /// </summary>
        /// <param name="client"></param>
        private void HandleGetSVars(NetConnection client)
        {
            //Check admin status -- only admins can get svars.
            IPlayerSession player = IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(client);
            if (!player.adminPermissions.isAdmin)
            {
                LogManager.Log("Player " + player.name + " tried to get SVars on Entity " + Owner.Uid + ", but is not an admin!", LogLevel.Warning);
            }
            else
            {
                SendSVars(client);
                LogManager.Log("Sending SVars to " + player.name + " for entity " + Owner.Uid + ":" + Owner.Name);
            }
        }
    }
}