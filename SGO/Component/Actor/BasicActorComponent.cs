using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared;
using ServerInterfaces;

namespace SGO
{
    public class BasicActorComponent : GameObjectComponent
    {
        IPlayerSession playerSession;

        public BasicActorComponent()
        {
            family = SS13_Shared.GO.ComponentFamily.Actor;
        }

        public override void SetParameter(ComponentParameter parameter)
        {
            switch (parameter.MemberName)
            {
                case "playersession":
                    if (parameter.ParameterType == typeof(IPlayerSession))
                        playerSession = (IPlayerSession)parameter.Parameter;
                    break;
                default:
                    base.SetParameter(parameter);
                    break;
            }
        }

        public override ComponentReplyMessage RecieveMessage(object sender, SS13_Shared.GO.ComponentMessageType type, params object[] list)
        {
            var reply = base.RecieveMessage(sender, type, list);

            if (sender == this)
                return ComponentReplyMessage.Empty;

            switch(type)
            {
                case SS13_Shared.GO.ComponentMessageType.GetActorConnection:
                    reply = new ComponentReplyMessage(SS13_Shared.GO.ComponentMessageType.ReturnActorConnection, playerSession.ConnectedClient);
                    break;
                case SS13_Shared.GO.ComponentMessageType.GetActorSession:
                    reply = new ComponentReplyMessage(SS13_Shared.GO.ComponentMessageType.ReturnActorSession, playerSession);
                    break;
            }

            return reply;
        }
    }
}
