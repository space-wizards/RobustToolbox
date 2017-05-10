using Lidgren.Network;
using SS14.Server.Interfaces.Player;
using SS14.Server.Services.Log;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.IoC;
using SS14.Shared.ServerEnums;
using System.Collections.Generic;

namespace SS14.Server.GameObjects
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
            IPlayerSession player = IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(client);

            Owner.GetComponent<Component>(parameter.Family).SetSVar(parameter);
            LogManager.Log("Player " + player.name + " set SVar."); //Make this message better
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
        /// </summary>
        /// <param name="client"></param>
        private void HandleGetSVars(NetConnection client)
        {
            IPlayerSession player = IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(client);
            SendSVars(client);
            LogManager.Log("Sending SVars to " + player.name + " for entity " + Owner.Uid + ":" + Owner.Name);
        }
    }
}
