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

        private readonly Dictionary<BoundKeyFunction, InputCmdHandler> _commandBinds = new Dictionary<BoundKeyFunction, InputCmdHandler>();

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

            // set state
            var states = GetInputStates(session);
            states.SetState(function, msg.State);

            // route the cmdMessage to the proper bind
            //Client Sanitization: unbound command, just ignore
            if (_commandBinds.TryGetValue(function, out var command))
            {
                command.HandleCmdMessage(session, msg);
            }
        }

        public IPlayerCommandStates GetInputStates(IPlayerSession session)
        {
            return _playerInputs[session];
        }

        public void BindFunction(BoundKeyFunction function, InputCmdHandler command)
        {
            _commandBinds.Add(function, command);
        }

        public void UnbindFunction(BoundKeyFunction function)
        {
            _commandBinds.Remove(function);
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
