using System;
using Robust.Shared.Network;
using Robust.Shared.ViewVariables;

namespace Robust.Server.ViewVariables;

internal sealed partial class ServerViewVariablesManager
{
    private void InitializeDomains()
    {
        RegisterDomain("player", ResolvePlayerObject);
    }

    private (ViewVariablesPath? Path, string[] Segments) ResolvePlayerObject(string path)
    {
        var empty = (new ViewVariablesInstancePath(_playerManager), Array.Empty<string>());

        if (string.IsNullOrEmpty(path))
            return empty;

        var segments = path.Split('/');

        if (segments.Length == 0)
            return empty;

        var identifier = segments[0];

        if (_playerManager.TryGetSessionByUsername(identifier, out var session))
            return (new ViewVariablesInstancePath(session), segments[1..]);

        if (_playerManager.TryGetPlayerDataByUsername(identifier, out var data))
            return (new ViewVariablesInstancePath(data), segments[1..]);

        if (!Guid.TryParse(identifier, out var guid))
            return EmptyResolve;

        var netId = new NetUserId(guid);

        if (_playerManager.TryGetSessionById(netId, out session))
            return (new ViewVariablesInstancePath(session), segments[1..]);

        if (_playerManager.TryGetPlayerData(netId, out data))
            return (new ViewVariablesInstancePath(data), segments[1..]);

        return EmptyResolve;
    }
}
