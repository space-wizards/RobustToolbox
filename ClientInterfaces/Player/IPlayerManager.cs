using System;
using ClientInterfaces.GOC;
using GameObject;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using Lidgren.Network;
using SS13_Shared;

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

        void ApplyPlayerStates(System.Collections.Generic.List<SS13_Shared.GameStates.PlayerState> list);
    }
}
