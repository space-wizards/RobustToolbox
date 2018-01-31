using System;
using SS14.Server.Player;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Players;

namespace SS14.Server.Interfaces.Player
{
    public interface IPlayerSession
    {
        IEntity AttachedEntity { get; }
        EntityUid? AttachedEntityUid { get; }
        string Name { get; set; }
        SessionStatus Status { get; set; }
        INetChannel ConnectedClient { get; }
        DateTime ConnectedTime { get; }

        PlayerIndex Index { get; }

        event EventHandler<SessionStatusEventArgs> PlayerStatusChanged;

        void JoinLobby();
        void JoinGame();

        void SetName(string name);

        void AttachToEntity(IEntity a);
        
        void DetachFromEntity();
        void OnConnect();
        void OnDisconnect();
        void AddPostProcessingEffect(PostProcessingEffectType type, float duration);
    }
}
