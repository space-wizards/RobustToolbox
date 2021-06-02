using System;
using System.Collections.Generic;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.IoC;

namespace Robust.Server.GameObjects
{
    /// <summary>
    ///     Server side processing of incoming user commands.
    /// </summary>
    public class InputSystem : SharedInputSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        private readonly Dictionary<IPlayerSession, IPlayerCommandStates> _playerInputs = new();


        private readonly Dictionary<IPlayerSession, uint> _lastProcessedInputCmd = new();

        /// <inheritdoc />
        public override void Initialize()
        {
            SubscribeNetworkEvent<FullInputCmdMessage>(InputMessageHandler);
            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            base.Shutdown();

            _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
        }

        private void InputMessageHandler(InputCmdMessage message, EntitySessionEventArgs eventArgs)
        {
            if (!(message is FullInputCmdMessage msg))
                return;

            //Client Sanitization: out of bounds functionID
            if (!_playerManager.KeyMap.TryGetKeyFunction(msg.InputFunctionId, out var function))
                return;

            //Client Sanitization: bad enum key state value
            if (!Enum.IsDefined(typeof(BoundKeyState), msg.State))
                return;

            var session = (IPlayerSession) eventArgs.SenderSession;

            if (_lastProcessedInputCmd[session] < msg.InputSequence)
                _lastProcessedInputCmd[session] = msg.InputSequence;

            // set state, only bound key functions get state changes
            var states = GetInputStates(session);
            states.SetState(function, msg.State);

            // route the cmdMessage to the proper bind
            //Client Sanitization: unbound command, just ignore
            foreach (var handler in BindRegistry.GetHandlers(function))
            {
                if (handler.HandleCmdMessage(session, msg)) return;
            }
        }

        public IPlayerCommandStates GetInputStates(IPlayerSession session)
        {
            return _playerInputs[session];
        }

        public uint GetLastInputCommand(IPlayerSession session)
        {
            return _lastProcessedInputCmd[session];
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
        {
            switch (args.NewStatus)
            {
                case SessionStatus.Connected:
                    _playerInputs.Add(args.Session, new PlayerCommandStates());
                    _lastProcessedInputCmd.Add(args.Session, 0);
                    break;

                case SessionStatus.Disconnected:
                    _playerInputs.Remove(args.Session);
                    _lastProcessedInputCmd.Remove(args.Session);
                    break;
            }
        }
    }
}
