using SS13_Shared.GO;
using ServerInterfaces;

namespace SGO
{
    public class BasicActorComponent : GameObjectComponent
    {
        private IPlayerSession playerSession;

        public BasicActorComponent()
        {
            family = ComponentFamily.Actor;
        }

        public override void SetParameter(ComponentParameter parameter)
        {
            switch (parameter.MemberName)
            {
                case "playersession":
                    if (parameter.ParameterType == typeof (IPlayerSession))
                        playerSession = (IPlayerSession) parameter.Parameter;
                    break;
                default:
                    base.SetParameter(parameter);
                    break;
            }
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this)
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.GetActorConnection:
                    reply = new ComponentReplyMessage(ComponentMessageType.ReturnActorConnection,
                                                      playerSession.ConnectedClient);
                    break;
                case ComponentMessageType.GetActorSession:
                    reply = new ComponentReplyMessage(ComponentMessageType.ReturnActorSession, playerSession);
                    break;
            }

            return reply;
        }
    }
}