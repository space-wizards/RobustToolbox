using Lidgren.Network;
using SS14.Server.Interfaces.Network;
using SS14.Server.Interfaces.Player;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.IoC;
using System;

namespace SS14.Server.GameObjects.Item.ItemCapability
{
    public class HealthScanCapability : ItemCapability
    {
        public HealthScanCapability()
        {
            CapabilityType = ItemCapabilityType.HealthScan;
            capabilityName = "HealthScanCapability";
            interactsWith = InteractsWith.Actor;
        }

        public override bool ApplyTo(Entity target, Entity sourceActor)
        {
            ComponentReplyMessage reply = sourceActor.SendMessage(this, ComponentFamily.Actor,
                                                                  ComponentMessageType.GetActorSession);

            if (reply.MessageType == ComponentMessageType.ReturnActorSession)
            {
                var session = (IPlayerSession) reply.ParamsList[0];
                var _netServer = IoCManager.Resolve<ISS14NetServer>();

                NetOutgoingMessage showScannerMsg = _netServer.CreateMessage();
                showScannerMsg.Write((byte) NetMessage.PlayerUiMessage);
                showScannerMsg.Write((byte) UiManagerMessage.CreateUiElement);
                showScannerMsg.Write((byte) CreateUiType.HealthScannerWindow);
                showScannerMsg.Write(target.Uid);
                _netServer.SendMessage(showScannerMsg, session.connectedClient, NetDeliveryMethod.ReliableUnordered);
                return true;
            }
            else
                throw new NotImplementedException("Actor has no session or No actor component that returns a session");
        }
    }
}