using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Generic;

[ToolshedCommand, MapLikeCommand(false)]
public sealed class EmplaceCommand : ToolshedCommand
{
    public override Type[] TypeParameterParsers => new[] {typeof(Type)};

    [CommandImplementation, TakesPipedTypeAsGeneric]
    TOut Emplace<TIn, TOut>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] TIn value,
        [CommandArgument] Block<TOut> block
    )
    {
        var emplaceCtx = new EmplaceContext<TIn>(ctx, value, EntityManager);
        return block.Invoke(null, emplaceCtx)!;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    IEnumerable<TOut> Emplace<TIn, TOut>(
            [CommandInvocationContext] IInvocationContext ctx,
            [PipedArgument] IEnumerable<TIn> value,
            [CommandArgument] Block<TOut> block
        )
    {

        foreach (var v in value)
        {
            var emplaceCtx = new EmplaceContext<TIn>(ctx, v, EntityManager);
            yield return block.Invoke(null, emplaceCtx)!;
        }
    }
}

internal record EmplaceContext<T>(IInvocationContext Inner, T Value, IEntityManager EntityManager) : IInvocationContext
{
    public bool CheckInvokable(CommandSpec command, out IConError? error)
    {
        return Inner.CheckInvokable(command, out error);
    }

    public ICommonSession? Session => Inner.Session;
    public ToolshedManager Toolshed => Inner.Toolshed;
    public NetUserId? User => Inner.User;

    public ToolshedEnvironment Environment => Inner.Environment;

    public void WriteLine(string line)
    {
        Inner.WriteLine(line);
    }

    public void ReportError(IConError err)
    {
        Inner.ReportError(err);
    }

    public IEnumerable<IConError> GetErrors()
    {
        return Inner.GetErrors();
    }

    public void ClearErrors()
    {
        Inner.ClearErrors();
    }

    public Dictionary<string, object?> Variables => default!; // we never use this.

    public IEnumerable<string> GetVars()
    {
        // note: this lies.
        return Inner.GetVars();
    }

    public object? ReadVar(string name)
    {
        if (name == "value")
            return Value;

        if (Value is IEmplaceBreakout breakout)
        {
            if (breakout.TryReadVar(name, out var value))
                return value;
        }

        if (Value is EntityUid id)
        {
            switch (name)
            {
                case "wy":
                case "wx":
                {
                    var xform = EntityManager.GetComponent<TransformComponent>(id);
                    var sys = EntityManager.System<SharedTransformSystem>();
                    var coords = sys.GetWorldPosition(xform);
                    if (name == "wx")
                        return coords.X;
                    else
                        return coords.Y;
                }
                case "proto":
                case "desc":
                case "name":
                case "paused":
                {
                    var meta = EntityManager.GetComponent<MetaDataComponent>(id);
                    switch (name)
                    {
                        case "proto":
                            return meta.EntityPrototype?.ID ?? "";
                        case "desc":
                            return meta.EntityDescription;
                        case "name":
                            return meta.EntityName;
                        case "paused":
                            return meta.EntityPaused;
                    }

                    throw new UnreachableException();
                }
            }
        }
        else if (Value is ICommonSession session)
        {
            switch (name)
            {
                case "ent":
                {
                    return EntityManager.GetNetEntity(session.AttachedEntity!);
                }
                case "name":
                {
                    return session.Name;
                }
                case "userid":
                {
                    return session.UserId;
                }
            }
        }

        return Inner.ReadVar(name);
    }

    public void WriteVar(string name, object? value)
    {
        if (name == "value")
            return;

        if (Value is IEmplaceBreakout v)
        {
            if (v.VarsOverriden.Contains(name))
                return;
        }

        Inner.WriteVar(name, value);
    }
}

public interface IEmplaceBreakout
{
    public ImmutableHashSet<string> VarsOverriden { get; }
    public bool TryReadVar(string name, out object? value);
}
