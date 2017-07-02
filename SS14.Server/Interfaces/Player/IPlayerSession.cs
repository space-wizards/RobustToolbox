using SS14.Shared;
using SS14.Shared.Interfaces.GameObjects;
using System;
using SS14.Shared.Network;
using SS14.Shared.Network.Messages;

namespace SS14.Server.Interfaces.Player
{
    public interface IPlayerSession
    {
        IEntity attachedEntity { get; }
        int? AttachedEntityUid { get; }
        string Name { get; set; }
        SessionStatus Status { get; set; }
        NetChannel ConnectedClient { get; }
        DateTime ConnectedTime { get; }

        void SetName(string name);

        void AttachToEntity(IEntity a);

        void HandleNetworkMessage(MsgSession message);

        void DetachFromEntity();
        void OnConnect();
        void OnDisconnect();
        void AddPostProcessingEffect(PostProcessingEffectType type, float duration);
    }
}
