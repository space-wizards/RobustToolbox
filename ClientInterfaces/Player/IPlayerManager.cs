using System;
using ClientInterfaces.GOC;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using Lidgren.Network;
using SS13_Shared;

namespace ClientInterfaces.Player
{
    public interface IPlayerManager
    {
        IEntity ControlledEntity { get; }

        event EventHandler<TypeEventArgs> RequestedStateSwitch;

        void Attach(IEntity newEntity);
        void Detach();
        void SendVerb(string verb, int uid);
        void KeyDown(KeyboardKeys key);
        void KeyUp(KeyboardKeys key);
        void HandleNetworkMessage(NetIncomingMessage message);
        void Update(float frameTime);
        void ApplyEffects(RenderImage image);
    }
}
