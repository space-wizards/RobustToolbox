using System;
using Robust.Shared.Network;

namespace Robust.Server.ViewVariables;

internal sealed partial class ViewVariablesHost
{
    private void InitializeDomains()
    {
        RegisterDomain("player", ResolvePlayerObject);
    }

    private (object? obj, string[] segments) ResolvePlayerObject(string path)
    {
        var empty = (_playerManager, Array.Empty<string>());

        if (string.IsNullOrEmpty(path))
            return empty;

        var segments = path.Split('/');

        if (segments.Length == 0)
            return empty;

        var identifier = segments[0];

        if (_playerManager.TryGetSessionByUsername(identifier, out var session))
            return (session, segments[1..]);

        if (_playerManager.TryGetPlayerDataByUsername(identifier, out var data))
            return (data, segments[1..]);

        if (!Guid.TryParse(identifier, out var guid))
            return EmptyResolve;

        var netId = new NetUserId(guid);

        if (_playerManager.TryGetSessionById(netId, out session))
            return (session, segments[1..]);

        if (_playerManager.TryGetPlayerData(netId, out data))
            return (data, segments[1..]);

        return EmptyResolve;
    }
}
