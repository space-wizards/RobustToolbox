using Lidgren.Network;
using SS14.Client.Interfaces.GOC;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;

namespace SS14.Client.GameObjects
{
    [IoCTarget]
    public class SVarsComponent : ClientComponent, ISVarsComponent
    {
        public override string Name => "SVars";
        public SVarsComponent()
        {
            Family = ComponentFamily.SVars;
        }

        #region ISVarsComponent Members

        public event EventHandler<GetSVarsEventArgs> GetSVarsCallback;

        public void DoSetSVar(MarshalComponentParameter svar)
        {
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, ComponentMessageType.SetSVar,
                                              svar.Serialize());
        }

        public void DoGetSVars()
        {
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, ComponentMessageType.GetSVars);
        }

        #endregion ISVarsComponent Members

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection sender)
        {
            switch ((ComponentMessageType)message.MessageParameters[0])
            {
                case ComponentMessageType.GetSVars:
                    HandleGetSVars(message);
                    break;
            }
        }

        public void HandleGetSVars(IncomingEntityComponentMessage message)
        {
            //If nothing's listening, then why bother with this shit?
            if (GetSVarsCallback == null)
                return;
            var count = (int)message.MessageParameters[1];
            var svars = new List<MarshalComponentParameter>();
            for (int i = 2; i < count + 2; i++)
            {
                svars.Add(MarshalComponentParameter.Deserialize((byte[])message.MessageParameters[i]));
            }

            GetSVarsCallback(this, new GetSVarsEventArgs(svars));
            GetSVarsCallback = null;
        }
    }
}
