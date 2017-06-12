using Lidgren.Network;
using SS14.Shared;
using SS14.Shared.Interfaces.GameObjects;
using System;

namespace SS14.Server.Interfaces.Player
{
    public interface IPlayerSession
    {
        NetConnection ConnectedClient { get; }
        IEntity attachedEntity { get; }
        int? AttachedEntityUid { get; }
        string name { get; set; }
        SessionStatus status { get; set; }
        NetConnection connectedClient { get; }
        DateTime ConnectedTime { get; }

        void SetName(string name);

        void AttachToEntity(IEntity a);

        void HandleNetworkMessage(NetIncomingMessage message);

        void DetachFromEntity();
        void OnConnect();
        void OnDisconnect();
        void AddPostProcessingEffect(PostProcessingEffectType type, float duration);
    }
}
