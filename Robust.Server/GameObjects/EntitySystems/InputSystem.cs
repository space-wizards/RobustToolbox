using System;
using System.Collections.Generic;
using Robust.Server.Interfaces.Player;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input;
using Robust.Shared.IoC;

namespace Robust.Server.GameObjects.EntitySystems
{
    /// <summary>
    ///     Server side processing of incoming user commands.
    /// </summary>
    public class InputSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IPlayerManager _playerManager;
#pragma warning restore 649

        private readonly Dictionary<IPlayerSession, IPlayerCommandStates> _playerInputs = new Dictionary<IPlayerSession, IPlayerCommandStates>();
        private readonly CommandBindMapping _bindMap = new CommandBindMapping();

        /// <summary>
        ///     Server side input command binds.
        /// </summary>
        public ICommandBindMapping BindMap => _bindMap;

        /// <inheritdoc />
        public override void Initialize()
        {
            SubscribeNetworkEvent<FullInputCmdMessage>(InputMessageHandler);
            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
        }

        private void InputMessageHandler(InputCmdMessage message)
        {
            var channel = message.NetChannel;
            if(channel == null)
                return;

            if (!(message is FullInputCmdMessage msg))
                return;

            //Client Sanitization: out of bounds functionID
            if (!_playerManager.KeyMap.TryGetKeyFunction(msg.InputFunctionId, out var function))
                return;

            var session = _playerManager.GetSessionByChannel(channel);

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
