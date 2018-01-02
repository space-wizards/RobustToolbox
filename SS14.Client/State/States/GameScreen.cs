using Lidgren.Network;
using SS14.Client.Input;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.Input;
using SS14.Client.UserInterface;
using SS14.Shared;
using SS14.Shared.Interfaces.Serialization;
using SS14.Shared.Configuration;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using System;
using System.Collections.Generic;

namespace SS14.Client.State.States
{
    // OH GOD.
    // Ok actually it's fine.
    public sealed partial class GameScreen : State
    {
        [Dependency]
        readonly ISS14Serializer _serializer;
        [Dependency]
        readonly IConfigurationManager _config;
        [Dependency]
        readonly IClientEntityManager _entityManager;
        [Dependency]
        readonly IComponentManager _componentManager;
        [Dependency]
        readonly IKeyBindingManager keyBindingManager;

        private EscapeMenu escapeMenu;

        public GameScreen(IDictionary<Type, object> managers) : base(managers)
        {
        }

        public override void InitializeGUI()
        {
            escapeMenu = new EscapeMenu();
            escapeMenu.Visible = false;
            escapeMenu.AddToScreen();
        }

        public override void Shutdown()
        {
            //throw new System.NotImplementedException();
        }

        public override void Startup()
        {
            IoCManager.InjectDependencies(this);

            _config.RegisterCVar("player.name", "Joe Genero", CVar.ARCHIVE);

            NetOutgoingMessage message = NetworkManager.CreateMessage();
            message.Write((byte)NetMessages.RequestMap);
            NetworkManager.ClientSendMessage(message, NetDeliveryMethod.ReliableUnordered);

            // TODO This should go somewhere else, there should be explicit session setup and teardown at some point.
            var message1 = NetworkManager.CreateMessage();
            message1.Write((byte)NetMessages.ClientName);
            message1.Write(ConfigurationManager.GetCVar<string>("player.name"));
            NetworkManager.ClientSendMessage(message1, NetDeliveryMethod.ReliableOrdered);
        }

        public override void Update(FrameEventArgs e)
        {
            _componentManager.Update(e.Elapsed);
            _entityManager.Update(e.Elapsed);
            //PlacementManager.Update(MousePosScreen);
            PlayerManager.Update(e.Elapsed);
        }

        public override void KeyDown(KeyEventArgs e)
        {
            Logger.Debug($"KeyDown! {e.Key}");
            if (e.Key == Keyboard.Key.Escape)
            {
                if (escapeMenu.Visible)
                {
                    if (escapeMenu.IsAtFront())
                    {
                        escapeMenu.Visible = false;
                    }
                    else
                    {
                        escapeMenu.MoveToFront();
                    }
                }
                else
                {
                    Logger.Debug("Opening!");
                    escapeMenu.OpenCentered();
                }

                e.Handle();
                return;
            }

            keyBindingManager.KeyDown(e);
        }

        public override void KeyUp(KeyEventArgs e)
        {
            keyBindingManager.KeyUp(e);
        }
    }
}
