using SS14.Server.Interfaces.GOC;
using SS14.Server.Interfaces.Player;
using SS14.Server.Interfaces.Round;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;


namespace SS14.Server.GameObjects
{
    public class BasicActorComponent : Component, IActorComponent
    {
        private IPlayerSession playerSession;

        public BasicActorComponent()
        {
            Family = ComponentFamily.Actor;
        }

        public override void SetParameter(ComponentParameter parameter)
        {
            switch (parameter.MemberName)
            {
                case "playersession": //TODO this shouldnt be a parameter.
                    playerSession = parameter.GetValue<IPlayerSession>();
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
                case ComponentMessageType.Die:
                    playerSession.AddPostProcessingEffect(PostProcessingEffectType.Death, -1);
                    IoCManager.Resolve<IRoundManager>().CurrentGameMode.PlayerDied(playerSession);
                    // Tell the current game mode a player just died
                    break;
            }

            return reply;
        }

        public IPlayerSession GetPlayerSession()
        {
            return playerSession;
        }
    }
}