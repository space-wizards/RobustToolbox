using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClientInterfaces.GOC;
using ClientInterfaces.Player;
using GameObject;
using Lidgren.Network;
using SS13.IoC;
using SS13_Shared;
using SS13_Shared.GO;
using SS13_Shared.ServerEnums;

namespace CGO
{
    public class SVarsComponent : Component, ISVarsComponent
    {
        public event EventHandler<GetSVarsEventArgs> GetSVarsCallback; 

        public SVarsComponent()
        {
            Family = ComponentFamily.SVars;
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection sender)
        {
            switch ((ComponentMessageType)message.MessageParameters[0])
            {
                case ComponentMessageType.GetSVars:
                    HandleGetSVars(message);
                    break;
            }
        }

        public void DoSetSVar(MarshalComponentParameter svar)
        {
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, svar.Serialize());
        }

        public void DoGetSVars()
        {
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, ComponentMessageType.GetSVars);
        }

        public void HandleGetSVars(IncomingEntityComponentMessage message)
        {
            //If nothing's listening, then why bother with this shit?
            if (GetSVarsCallback == null)
                return;
            var count = (int)message.MessageParameters[1];
            var svars = new List<MarshalComponentParameter>();
            for(int i = 2;i < count + 2; i++)
            {
                svars.Add(MarshalComponentParameter.Deserialize((byte[])message.MessageParameters[i]));
            }

            GetSVarsCallback(this, new GetSVarsEventArgs(svars));
            GetSVarsCallback = null;
        }
    }
}
