using System;
using Robust.Shared.Network.Messages;
using Robust.Shared.ViewVariables;

namespace Robust.Server.ViewVariables
{
    internal sealed partial class ServerViewVariablesManager
    {
        // The server can see both the shared docs and its own server-only docs.
        private ViewVariablesDocStrings? _docStrings;

        /// <summary>
        ///     Keys longer than this are skipped. Real member names are nowhere near this long.
        /// </summary>
        private const int MaxDocStringKeyLength = 1024;

        private void _msgReqDocStrings(MsgViewVariablesReqDocStrings message)
        {
            // ReadFromBuffer already checks these, but don't trust a message that skipped it.
            var keys = message.Keys;
            if (keys == null || keys.Count > MsgViewVariablesReqDocStrings.MaxKeys)
            {
                Sawmill.Warning($"{message.MsgChannel} sent a DocString request with an invalid key count!");
                return;
            }

            var response = new MsgViewVariablesDocStrings
            {
                RequestId = message.RequestId,
                // The client treats a short answer as "missing" for every key and doesn't cache it,
                // so clients who fail vv the check gets the shared empty array.
                DocStrings = Array.Empty<string?>(),
            };

            // Only players who can VV get to read server docs.
            if (CheckPermissions(message.MsgChannel, "vv")
                && message.Kind is ViewVariablesDocStringKind.Type or ViewVariablesDocStringKind.FieldOrProperty)
            {
                // Loaded on first use so servers that never get VV'd don't pay for it.
                _docStrings ??= new(Sawmill, ViewVariablesDocStrings.SharedRoot, ViewVariablesDocStrings.ServerRoot);
                _docStrings.EnsureLoaded(_resManager);

                response.DocStrings = new string?[keys.Count];
                for (var i = 0; i < keys.Count; i++)
                {
                    var key = keys[i];
                    if (string.IsNullOrEmpty(key) || key.Length > MaxDocStringKeyLength)
                        continue;

                    if (_docStrings.TryGet(message.Kind, key, out var docString))
                        response.DocStrings[i] = docString;
                }
            }

            _netManager.ServerSendMessage(response, message.MsgChannel);
        }
    }
}
