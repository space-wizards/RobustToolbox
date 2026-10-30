using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.ViewVariables;

namespace Robust.Client.ViewVariables
{
    internal sealed partial class ClientViewVariablesManager
    {
        // The client only ever sees the shared docs, server docs are requested from the server.
        private ViewVariablesDocStrings? _docStrings = null;

        // DocStrings we already asked the server for. null means the server didn't have one.
        // Cleared on disconnect because a different server may have different code.
        private readonly Dictionary<(ViewVariablesDocStringKind, string), string?> _remoteDocStrings = new();

        private readonly Dictionary<uint, TaskCompletionSource<string?[]>> _requestedDocStrings = new();

        public void LoadDocStrings()
        {
            _docStrings = new(Sawmill, ViewVariablesDocStrings.SharedRoot);
            _docStrings.EnsureLoaded(_resManager);
        }

        public string GetDocStringForType(Type type)
        {
            return GetDocStringForType(DocStringKeyForType(type));
        }

        public string GetDocStringForType(string key)
        {
            return GetLocalDocString(ViewVariablesDocStringKind.Type, key);
        }

        public string GetDocStringForFieldOrProperty(string key)
        {
            return GetLocalDocString(ViewVariablesDocStringKind.FieldOrProperty, key);
        }

        public async Task<string[]> GetDocStringsAsync(ViewVariablesDocStringKind kind, IReadOnlyList<string> keys)
        {
            LoadDocStrings();

            var result = new string[keys.Count];
            var missingKeys = new List<string>();
            var missingIdx = new List<int>();

            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];

                if (_docStrings!.TryGet(kind, key, out var local))
                {
                    result[i] = local;
                }
                else if (_remoteDocStrings.TryGetValue((kind, key), out var cached))
                {
                    result[i] = cached ?? ViewVariablesDocStrings.Missing(key);
                }
                else
                {
                    missingKeys.Add(key);
                    missingIdx.Add(i);
                }
            }

            if (missingKeys.Count == 0 || !_netManager.IsConnected)
            {
                foreach (var i in missingIdx)
                {
                    result[i] = ViewVariablesDocStrings.Missing(keys[i]);
                }
                return result;
            }

            for (var start = 0; start < missingKeys.Count; start += MsgViewVariablesReqDocStrings.MaxKeys)
            {
                // We may have disconnected while waiting on the previous chunk.
                if (!_netManager.IsConnected)
                {
                    for (var k = start; k < missingKeys.Count; k++)
                    {
                        result[missingIdx[k]] = ViewVariablesDocStrings.Missing(missingKeys[k]);
                    }
                    break;
                }

                var count = Math.Min(MsgViewVariablesReqDocStrings.MaxKeys, missingKeys.Count - start);
                var chunk = missingKeys.GetRange(start, count);
                var remote = await RequestDocStrings(kind, chunk);

                for (var j = 0; j < chunk.Count; j++)
                {
                    // A short answer means we never heard back for this key (e.g. we disconnected and the
                    // request was finished with an empty array). Don't cache it, or the miss would stick
                    // around after the cache was cleared and carry over to the next server.
                    if (j >= remote.Length)
                    {
                        result[missingIdx[start + j]] = ViewVariablesDocStrings.Missing(chunk[j]);
                        continue;
                    }

                    var docString = remote[j];
                    _remoteDocStrings[(kind, chunk[j])] = docString;
                    result[missingIdx[start + j]] = docString ?? ViewVariablesDocStrings.Missing(chunk[j]);
                }
            }

            return result;
        }

        public async void ApplyDocStringsAsync(ViewVariablesDocStringKind kind, IReadOnlyList<(string Key, Action<string> Apply)> targets)
        {
            if (targets.Count == 0)
                return;

            var keys = new string[targets.Count];
            for (var i = 0; i < targets.Count; i++)
            {
                keys[i] = targets[i].Key;
            }

            string[] docStrings;
            try
            {
                docStrings = await GetDocStringsAsync(kind, keys);
            }
            catch (Exception e)
            {
                Sawmill.Error($"Failed to get DocStrings: {e}");
                return;
            }

            for (var i = 0; i < targets.Count; i++)
            {
                targets[i].Apply(docStrings[i]);
            }
        }

        public static string DocStringKeyForType(Type type)
        {
            return type.AssemblyQualifiedName!.Split(", ")[0];
        }

        private string GetLocalDocString(ViewVariablesDocStringKind kind, string key)
        {
            LoadDocStrings();

            return _docStrings!.TryGet(kind, key, out var docString)
                ? docString
                : ViewVariablesDocStrings.Missing(key);
        }

        private Task<string?[]> RequestDocStrings(ViewVariablesDocStringKind kind, List<string> keys)
        {
            var msg = new MsgViewVariablesReqDocStrings
            {
                RequestId = _nextReqId++,
                Kind = kind,
                Keys = keys,
            };

            var tcs = new TaskCompletionSource<string?[]>();
            _requestedDocStrings.Add(msg.RequestId, tcs);
            _netManager.ClientSendMessage(msg);
            return tcs.Task;
        }

        private void _netMessageDocStrings(MsgViewVariablesDocStrings message)
        {
            if (!_requestedDocStrings.Remove(message.RequestId, out var tcs))
            {
                Sawmill.Error($"Server sent us DocStrings we never asked for, request ID {message.RequestId}.");
                return;
            }

            tcs.SetResult(message.DocStrings);
        }

        private void OnDisconnectDocStrings(object? sender, NetDisconnectedArgs e)
        {
            _remoteDocStrings.Clear();

            // Nobody is going to answer these anymore, finish them with empty results.
            foreach (var tcs in _requestedDocStrings.Values)
            {
                tcs.TrySetResult(Array.Empty<string?>());
            }
            _requestedDocStrings.Clear();
        }
    }
}
