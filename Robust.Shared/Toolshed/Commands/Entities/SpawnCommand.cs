using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Robust.Shared.Toolshed.Commands.Entities;

[ToolshedCommand]
internal sealed class SpawnCommand : ToolshedCommand
{
    #region spawn:at implementations
    [CommandImplementation("at")]
    public EntityUid SpawnAt([PipedArgument] EntityCoordinates target, EntProtoId proto)
    {
        return Spawn(proto, target);
    }

    [CommandImplementation("at")]
    public IEnumerable<EntityUid> SpawnAt([PipedArgument] IEnumerable<EntityCoordinates> target, EntProtoId proto)
        => target.Select(x => SpawnAt(x, proto));
    #endregion

    #region spawn:on implementations
    [CommandImplementation("on")]
    public EntityUid SpawnOn([PipedArgument] EntityUid target, EntProtoId proto)
    {
        return Spawn(proto, Transform(target).Coordinates);
    }

    [CommandImplementation("on")]
    public IEnumerable<EntityUid> SpawnOn([PipedArgument] IEnumerable<EntityUid> target, EntProtoId proto)
        => target.Select(x => SpawnOn(x, proto));
    #endregion

    #region spawn:attached implementations
    [CommandImplementation("attached")]
    public EntityUid SpawnIn([PipedArgument] EntityUid target, EntProtoId proto)
    {
        return Spawn(proto, new EntityCoordinates(target, Vector2.Zero));
    }

    [CommandImplementation("attached")]
    public IEnumerable<EntityUid> SpawnIn([PipedArgument] IEnumerable<EntityUid> target, EntProtoId proto)
        => target.Select(x => SpawnIn(x, proto));
    #endregion
}
