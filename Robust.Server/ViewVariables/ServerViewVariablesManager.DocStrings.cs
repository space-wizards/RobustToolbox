using Robust.Shared.Network.Messages;
using Robust.Shared.ViewVariables;

namespace Robust.Server.ViewVariables
{
    internal sealed partial class ServerViewVariablesManager
    {
        // The server can see both the shared docs and its own server-only docs.
        private readonly ViewVariablesDocStrings _docStrings =
            new(ViewVariablesDocStrings.SharedRoot, ViewVariablesDocStrings.ServerRoot);

        private void _msgReqDocStrings(MsgViewVariablesReqDocStrings message)
        {
            var response = new MsgViewVariablesDocStrings
            {
                RequestId = message.RequestId,
                DocStrings = new string?[message.Keys.Count],
            };

            // Only players who can VV get to read server docs. Everyone else gets an all-null answer
            // so the client's pending request still completes.
            var player = _playerManager.GetSessionByChannel(message.MsgChannel);
            if (_groupController.CanCommand(player, "vv"))
            {
                // Loaded on first use so servers that never get VV'd don't pay for it.
                _docStrings.EnsureLoaded(_resManager, Sawmill);

                for (var i = 0; i < message.Keys.Count; i++)
                {
                    if (_docStrings.TryGet(message.Kind, message.Keys[i], out var docString))
                        response.DocStrings[i] = docString;
                }
            }

            _netManager.ServerSendMessage(response, message.MsgChannel);
        }
    }
}
