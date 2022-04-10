using System;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;

namespace Robust.Client.Console
{
    public sealed partial class ScriptClient : IScriptClient
    {
        [Dependency] private readonly IClientConGroupController _conGroupController = default!;
        [Dependency] private readonly IClientNetManager _netManager = default!;

        private readonly Dictionary<int, ScriptConsoleServer> _activeConsoles = new();

        private int _nextSessionId = 1;

        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgScriptStop>();
            _netManager.RegisterNetMessage<MsgScriptEval>();
            _netManager.RegisterNetMessage<MsgScriptStart>();
            _netManager.RegisterNetMessage<MsgScriptCompletion>();
            _netManager.RegisterNetMessage<MsgScriptCompletionResponse>(ReceiveScriptCompletionResponse);
            _netManager.RegisterNetMessage<MsgScriptResponse>(ReceiveScriptResponse);
            _netManager.RegisterNetMessage<MsgScriptStartAck>(ReceiveScriptStartAckResponse);
        }

        private void ReceiveScriptStartAckResponse(MsgScriptStartAck message)
        {
            var session = message.ScriptSession;

            var console = new ScriptConsoleServer(this, session);
            _activeConsoles.Add(session, console);
            // FIXME: When this is Open(), resizing the window will cause its position to get NaN'd.
            console.OpenCentered();
        }

        private void ReceiveScriptResponse(MsgScriptResponse message)
        {
            if (!_activeConsoles.TryGetValue(message.ScriptSession, out var console))
            {
                return;
            }

            console.ReceiveResponse(message);
        }

        private void ReceiveScriptCompletionResponse(MsgScriptCompletionResponse message)
        {
            if (!_activeConsoles.TryGetValue(message.ScriptSession, out var console))
            {
                return;
            }

            console.ReceiveCompletionResponse(message);
        }

        public bool CanScript => _conGroupController.CanScript();

        public void StartSession()
        {
            if (!CanScript)
            {
                throw new InvalidOperationException("We do not have scripting permission.");
            }

            var msg = _netManager.CreateNetMessage<MsgScriptStart>();
            msg.ScriptSession = _nextSessionId++;
            _netManager.ClientSendMessage(msg);
        }

        private void ConsoleClosed(int session)
        {
            _activeConsoles.Remove(session);

            var msg = _netManager.CreateNetMessage<MsgScriptStop>();
            msg.ScriptSession = session;
            _netManager.ClientSendMessage(msg);
        }
    }
}
