using System;
using System.Collections.Generic;
using SS14.Server.Interfaces.Player;
using SS14.Server.Player;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Systems;
using SS14.Shared.Input;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;

namespace SS14.Server.GameObjects.EntitySystems
{
    /// <summary>
    ///     Server side processing of incoming user commands.
    /// </summary>
    public class InputSystem : EntitySystem
    {
        private readonly Dictionary<IPlayerSession, IPlayerCommandStates> _playerInputs = new Dictionary<IPlayerSession, IPlayerCommandStates>();
        private readonly CommandBindMapping _bindMap = new CommandBindMapping();

        /// <summary>
        ///     Server side input command binds.
        /// </summary>
        public ICommandBindMapping BindMap => _bindMap;

        /// <inheritdoc />
        public override void RegisterMessageTypes()
        {
            RegisterMessageType<FullInputCmdMessage>();
        }

        /// <inheritdoc />
        public override void Initialize()
        {
            IoCManager.Resolve<IPlayerManager>().PlayerStatusChanged += OnPlayerStatusChanged;
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            IoCManager.Resolve<IPlayerManager>().PlayerStatusChanged -= OnPlayerStatusChanged;
        }

        /// <inheritdoc />
        public override void HandleNetMessage(INetChannel channel, EntitySystemMessage message)
        {
            if (!(message is FullInputCmdMessage msg))
                return;

            //Client Sanitization: out of bounds functionID
            if (!IoCManager.Resolve<IPlayerManager>().KeyMap.TryGetKeyFunction(msg.InputFunctionId, out var function))
                return;

            var session = IoCManager.Resolve<IPlayerManager>().GetSessionByChannel(channel);

            //Client Sanitization: bad enum key state value
            if (!Enum.IsDefined(typeof(BoundKeyState), msg.State))
                return;
            
            // route the cmdMessage to the proper bind
            //Client Sanitization: unbound command, just ignore
            if (_bindMap.TryGetHandler(function, out var command))
            {
                // set state, only bound key functions get state changes
                var states = GetInputStates(session);
                states.SetState(function, msg.State);

                command.HandleCmdMessage(session, msg);
            }
        }

        public IPlayerCommandStates GetInputStates(IPlayerSession session)
        {
            return _playerInputs[session];
        }
        
        private void OnPlayerStatusChanged(object sender, SessionStatusEventArgs args)
        {
            switch (args.NewStatus)
            {
                case SessionStatus.Connected:
                    _playerInputs.Add(args.Session, new PlayerCommandStates());
                    break;

                case SessionStatus.Disconnected:
                    _playerInputs.Remove(args.Session);
                    break;
            }
        }
    }
}
