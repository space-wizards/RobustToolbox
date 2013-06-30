using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using SS13_Shared;
using ServerInterfaces.GameObject;

namespace ServerInterfaces.Player
{
    public interface IPlayerSession
    {
        NetConnection ConnectedClient { get; }
        BodyPart TargetedArea { get; }
        IEntity attachedEntity { get; }
        AdminPermissions adminPermissions { get; }
        string name { get; set; }
        SessionStatus status { get; set; }
        JobDefinition assignedJob { get; set; }
        NetConnection connectedClient { get; }

        void SetName(string name);

        void AttachToEntity(IEntity a);

        void HandleNetworkMessage(NetIncomingMessage message);

        void DetachFromEntity();
        void OnConnect();
        void OnDisconnect();
        void AddPostProcessingEffect(PostProcessingEffectType type, float duration);
    }
}
