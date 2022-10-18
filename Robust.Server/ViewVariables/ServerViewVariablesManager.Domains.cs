using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Network;
using Robust.Shared.ViewVariables;

namespace Robust.Server.ViewVariables;

internal sealed partial class ServerViewVariablesManager
{
    private void InitializeDomains()
    {
        RegisterDomain("player", ResolvePlayerObject, ListPlayerPaths);
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

    private IEnumerable<string>? ListPlayerPaths(string[] segments)
    {
        if (segments.Length > 1)
            return null;

        if (segments.Length == 1
            && (_playerManager.TryGetSessionByUsername(segments[0], out _)
            || Guid.TryParse(segments[0], out var guid)
            && _playerManager.TryGetSessionById(new NetUserId(guid), out _)))
        {
            return null;
        }

        return _playerManager.Sessions
            .Select(s => s.Name)
            .Concat(_playerManager.Sessions
                .Select(s => s.UserId.UserId.ToString()));
    }
}
