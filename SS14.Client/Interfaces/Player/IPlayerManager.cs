using System;
using System.Collections.Generic;
using Lidgren.Network;
using SS14.Client.Graphics.Render;
using SS14.Client.Player;
using SS14.Shared;
using SS14.Shared.GameStates;

namespace SS14.Client.Interfaces.Player
{
    public interface IPlayerManager
    {
        LocalPlayer LocalPlayer { get; }

        event EventHandler<TypeEventArgs> RequestedStateSwitch;

        void SendVerb(string verb, int uid);
        void HandleNetworkMessage(NetIncomingMessage message);
        void Update(float frameTime);
        void ApplyEffects(RenderImage image);

        void ApplyPlayerStates(List<PlayerState> list);
    }
}
