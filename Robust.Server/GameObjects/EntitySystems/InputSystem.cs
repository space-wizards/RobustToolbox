using System;
using System.Collections.Generic;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.GameObjects
{
    /// <summary>
    ///     Server side processing of incoming user commands.
    /// </summary>
    public sealed class InputSystem : SharedInputSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IServerNetManager _netMgr = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly INetConfigurationManager _configurationManager = default!;

        private bool _logLateMsgs;
        private ISawmill _sawmill = default!;
        private readonly Dictionary<IPlayerSession, PlayerInputData> _playerData = new();

        /// <inheritdoc />
        public override void Initialize()
        {
            _netMgr.RegisterNetMessage<MsgInput>(OnInputMsg);
            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
            _configurationManager.OnValueChanged(CVars.NetLogLateMsg, b => _logLateMsgs = b, true);
            _sawmill = Logger.GetSawmill("input");

            SubscribeLocalEvent<BeforeTickUpdateEvent>(ProcessInputs);
        }

        private void ProcessInputs(BeforeTickUpdateEvent args)
        {
            foreach (var data in _playerData.Values)
            {
                while (data.Queue.Count > 0)
                {
                    var inputMsg = data.Queue.Peek();

                    if (inputMsg.InputSequence != data.LastProcessedSequence + 1 || inputMsg.Tick > _gameTiming.CurTick)
                        break;

                    data.Queue.Take();
                    data.QueuedSequences.Remove(inputMsg.InputSequence);
                    InputMessageHandler(inputMsg, data);
                }
            }
        }

        private void OnInputMsg(MsgInput message)
        {
            var session = _playerManager.GetSessionByChannel(message.MsgChannel);

            if (!_playerData.TryGetValue(session, out var data))
            {
                _sawmill.Error($"Got input message from a disconnected player? Session: {session}");
                return;
            }

            foreach (var inputMsg in message.InputMessageList.Span)
            {
                if (inputMsg.InputSequence <= data.LastProcessedSequence)
                    continue;

                if (!data.QueuedSequences.Add(inputMsg.InputSequence))
                    continue;

                data.Queue.Add(inputMsg);

                if (inputMsg.Tick < _gameTiming.CurTick && _logLateMsgs && _configurationManager.GetClientCVar(message.MsgChannel, CVars.NetPredict))
                {
                    _sawmill.Warning("Got late input message! Diff: {0}, msgT: {2}, cT: {3}, player: {1}",
                        (int)inputMsg.Tick.Value - (int)_gameTiming.CurTick.Value, message.MsgChannel.UserName, inputMsg.Tick, _gameTiming.CurTick);
                }
            }
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            base.Shutdown();

            _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
        }

        private void InputMessageHandler(FullInputCmdMessage msg, PlayerInputData data)
        {
            data.LastProcessedSequence = msg.InputSequence;

            //Client Sanitization: out of bounds functionID
            if (!_playerManager.KeyMap.TryGetKeyFunction(msg.InputFunctionId, out var function))
                return;

            //Client Sanitization: bad enum key state value
            if (!Enum.IsDefined(typeof(BoundKeyState), msg.State))
                return;

            // set state, only bound key functions get state changes
            var states = GetInputStates(data.Session);
            states.SetState(function, msg.State);

            // route the cmdMessage to the proper bind
            //Client Sanitization: unbound command, just ignore
            foreach (var handler in BindRegistry.GetHandlers(function))
            {
                if (handler.HandleCmdMessage(data.Session, msg)) return;
            }
        }

        public IPlayerCommandStates GetInputStates(IPlayerSession session)
        {
            return _playerData[session].State;
        }

        /// <summary>
        ///     Find the largest consecutive input sequence.
        /// </summary>
        public uint GetLastApplicableInputCommand(IPlayerSession session)
        {
            var data = _playerData[session];
            var last = data.LastProcessedSequence;

            // We may have additional inputs that have not yet been processed.
            foreach (var msg in data.Queue)
            {
                if (msg.InputSequence == last + 1)
                    last++;
            }
            return last;
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
        {
            switch (args.NewStatus)
            {
                case SessionStatus.Connected:
                    _playerData.Add(args.Session, new(args.Session));
                    break;

                case SessionStatus.Disconnected:
                    _playerData.Remove(args.Session);
                    break;
            }
        }
    }

    public sealed class PlayerInputData
    {
        public PlayerInputData(IPlayerSession session)
        {
            Session = session;
        }

        public readonly HashSet<uint> QueuedSequences = new();
        public readonly PriorityQueue<FullInputCmdMessage> Queue = new();
        public uint LastProcessedSequence = 0;
        public readonly IPlayerCommandStates State = new PlayerCommandStates();
        public readonly IPlayerSession Session;
    }
}
