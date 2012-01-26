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
            family = SS3D_shared.GO.ComponentFamily.Actor;
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

        public override void RecieveMessage(object sender, SS3D_shared.GO.ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            base.RecieveMessage(sender, type, replies, list);

            switch(type)
            {
                case SS3D_shared.GO.ComponentMessageType.GetActorConnection:
                    replies.Add(new ComponentReplyMessage(SS3D_shared.GO.ComponentMessageType.ReturnActorConnection, playerSession.ConnectedClient));
                    break;
                case SS3D_shared.GO.ComponentMessageType.GetActorSession:
                    replies.Add(new ComponentReplyMessage(SS3D_shared.GO.ComponentMessageType.ReturnActorSession, playerSession));
                    break;
            }
        }
    }
}
