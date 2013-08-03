using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using SS13_Shared;
using ServerInterfaces.GOC;
using GO = GameObject;

namespace ServerInterfaces.Player
{
    public interface IPlayerSession
    {
        NetConnection ConnectedClient { get; }
        BodyPart TargetedArea { get; }
        GO.Entity attachedEntity { get; }
        AdminPermissions adminPermissions { get; }
        string name { get; set; }
        SessionStatus status { get; set; }
        JobDefinition assignedJob { get; set; }
        NetConnection connectedClient { get; }
        DateTime ConnectedTime { get; }

        void SetName(string name);

        void AttachToEntity(GO.Entity a);

        void HandleNetworkMessage(NetIncomingMessage message);

        void DetachFromEntity();
        void OnConnect();
        void OnDisconnect();
        void AddPostProcessingEffect(PostProcessingEffectType type, float duration);
    }
}
