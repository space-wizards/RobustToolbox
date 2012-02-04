using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public override void RecieveMessage(object sender, SS13_Shared.GO.ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            base.RecieveMessage(sender, type, replies, list);

            switch(type)
            {
                case SS13_Shared.GO.ComponentMessageType.GetActorConnection:
                    replies.Add(new ComponentReplyMessage(SS13_Shared.GO.ComponentMessageType.ReturnActorConnection, playerSession.ConnectedClient));
                    break;
                case SS13_Shared.GO.ComponentMessageType.GetActorSession:
                    replies.Add(new ComponentReplyMessage(SS13_Shared.GO.ComponentMessageType.ReturnActorSession, playerSession));
                    break;
            }
        }
    }
}
