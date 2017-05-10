using Lidgren.Network;
using SS14.Shared;
using System;
using GO = SS14.Shared.GameObjects;

namespace SS14.Server.Interfaces.Player
{
    public interface IPlayerSession
    {
        NetConnection ConnectedClient { get; }
        BodyPart TargetedArea { get; }
        GO.Entity attachedEntity { get; }
        int? AttachedEntityUid { get; }
        string name { get; set; }
        SessionStatus status { get; set; }
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
