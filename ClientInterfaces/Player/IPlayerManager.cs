using System;
using System.Collections.Generic;
using GameObject;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GameStates;

namespace ClientInterfaces.Player
{
    public interface IPlayerManager
    {
        Entity ControlledEntity { get; }

        event EventHandler<TypeEventArgs> RequestedStateSwitch;
        event EventHandler<VectorEventArgs> OnPlayerMove;

        void Attach(Entity newEntity);
        void Detach();
        void SendVerb(string verb, int uid);
        void KeyDown(KeyboardKeys key);
        void KeyUp(KeyboardKeys key);
        void HandleNetworkMessage(NetIncomingMessage message);
        void Update(float frameTime);
        void ApplyEffects(RenderImage image);

        void ApplyPlayerStates(List<PlayerState> list);
    }
}