using System;
using SS14.Server.Player;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Players;

namespace SS14.Server.Interfaces.Player
{
    public interface IPlayerSession : ICommonSession
    {
        IEntity AttachedEntity { get; }
        EntityUid? AttachedEntityUid { get; }
        INetChannel ConnectedClient { get; }
        DateTime ConnectedTime { get; }

        event EventHandler<SessionStatusEventArgs> PlayerStatusChanged;

        void JoinLobby();
        void JoinGame();

        void AttachToEntity(IEntity a);

        void DetachFromEntity();
        void OnConnect();
        void OnDisconnect();
        void AddPostProcessingEffect(PostProcessingEffectType type, float duration);

        IPlayerData Data { get; }
    }
}
