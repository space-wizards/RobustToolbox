using System;
using System.Collections.Generic;
using Lidgren.Network;
using SS14.Client.Graphics.Render;
using SS14.Shared;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces.GameObjects;

namespace SS14.Client.Interfaces.Player
{
    public interface IPlayerManager
    {
        IEntity ControlledEntity { get; }

        event EventHandler<TypeEventArgs> RequestedStateSwitch;
        event EventHandler<MoveEventArgs> OnPlayerMove;

        void Attach(IEntity newEntity);
        void Detach();
        void SendVerb(string verb, int uid);
        void HandleNetworkMessage(NetIncomingMessage message);
        void Update(float frameTime);
        void ApplyEffects(RenderImage image);

        void ApplyPlayerStates(List<PlayerState> list);
    }
}
