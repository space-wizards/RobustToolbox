using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;

namespace Robust.Shared.Toolshed.Commands.Entities;

[ToolshedCommand]
internal sealed class SpawnCommand : ToolshedCommand
{
    #region spawn:at implementations
    [CommandImplementation("at")]
    public EntityUid SpawnAt(
            [CommandInvocationContext] IInvocationContext ctx,
            [PipedArgument] EntityCoordinates target,
            [CommandArgument] ValueRef<string, Prototype<EntityPrototype>> proto
        )
    {
        return Spawn(proto.Evaluate(ctx), target);
    }

    [CommandImplementation("at")]
    public IEnumerable<EntityUid> SpawnAt(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityCoordinates> target,
        [CommandArgument] ValueRef<string, Prototype<EntityPrototype>> proto
    )
        => target.Select(x => SpawnAt(ctx, x, proto));
    #endregion

    #region spawn:on implementations
    [CommandImplementation("on")]
    public EntityUid SpawnOn(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid target,
        [CommandArgument] ValueRef<string, Prototype<EntityPrototype>> proto
    )
    {
        return Spawn(proto.Evaluate(ctx), Transform(target).Coordinates);
    }

    [CommandImplementation("on")]
    public IEnumerable<EntityUid> SpawnOn(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> target,
        [CommandArgument] ValueRef<string, Prototype<EntityPrototype>> proto
    )
        => target.Select(x => SpawnOn(ctx, x, proto));
    #endregion

    #region spawn:attached implementations
    [CommandImplementation("attached")]
    public EntityUid SpawnIn(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid target,
        [CommandArgument] ValueRef<string, Prototype<EntityPrototype>> proto
    )
    {
        return Spawn(proto.Evaluate(ctx), new EntityCoordinates(target, Vector2.Zero));
    }

    [CommandImplementation("attached")]
    public IEnumerable<EntityUid> SpawnIn(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> target,
        [CommandArgument] ValueRef<string, Prototype<EntityPrototype>> proto
    )
        => target.Select(x => SpawnIn(ctx, x, proto));
    #endregion
}
