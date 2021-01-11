using System;
using System.Collections.Generic;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Network.Messages;

namespace Robust.Client.Console
{
    public partial class ScriptClient : IScriptClient
    {
        [Dependency] private readonly IClientConGroupController _conGroupController = default!;
        [Dependency] private readonly IClientNetManager _netManager = default!;

        private readonly Dictionary<int, ScriptConsoleServer> _activeConsoles = new();

        private int _nextSessionId = 1;

        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgScriptStop>(MsgScriptStop.NAME);
            _netManager.RegisterNetMessage<MsgScriptEval>(MsgScriptEval.NAME);
            _netManager.RegisterNetMessage<MsgScriptStart>(MsgScriptStart.NAME);
            _netManager.RegisterNetMessage<MsgScriptResponse>(MsgScriptResponse.NAME, ReceiveScriptResponse);
            _netManager.RegisterNetMessage<MsgScriptStartAck>(MsgScriptStartAck.NAME, ReceiveScriptStartAckResponse);
        }

        private void ReceiveScriptStartAckResponse(MsgScriptStartAck message)
        {
            var session = message.ScriptSession;

            var console = new ScriptConsoleServer(this, session);
            _activeConsoles.Add(session, console);
            console.Open();
        }

        private void ReceiveScriptResponse(MsgScriptResponse message)
        {
            if (!_activeConsoles.TryGetValue(message.ScriptSession, out var console))
            {
                return;
            }

            console.ReceiveResponse(message);
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
