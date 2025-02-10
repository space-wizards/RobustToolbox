using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;

namespace Robust.Shared.Toolshed.Commands.Math;

[ToolshedCommand(Name = "?")]
public sealed class DefaultIfNullCommand : ToolshedCommand
{
    // TODO TOOLSHED fix PipedBlockType
    // This command will currently fail if the Tin == typeof(IEnumerable<>)
    private static Type[] _parsers = [typeof(MapBlockOutputParser)];
    public override Type[] TypeParameterParsers => _parsers;

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public TOut? DefaultIfNull<TOut, TIn>(
            IInvocationContext ctx,
            [PipedArgument] TIn? value,
            Block<TIn, TOut> follower
        )
        where TIn : unmanaged
    {
        if (value is null)
            return default;

        return follower.Invoke(value.Value, ctx);
    }
}

[ToolshedCommand(Name = "or?")]
public sealed class OrValueCommand : ToolshedCommand
{
    // Yes, these really do have different signatures.

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T OrValue<T>(
            IInvocationContext ctx,
            [PipedArgument] T? value,
            ValueRef<T> alternate
        )
        where T : class
    {
        return value ?? alternate.Evaluate(ctx)!;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T OrValue<T>(
            IInvocationContext ctx,
            [PipedArgument] T? value,
            ValueRef<T> alternate
        )
        where T : unmanaged
    {
        if (value == null)
            return alternate.Evaluate(ctx);
        return value.Value;
    }
}

[ToolshedCommand(Name = "??")]
public sealed class DebugPrintCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T DebugPrint<T>(
            IInvocationContext ctx,
            [PipedArgument] T value
        )
    {
        ctx.WriteLine(Toolshed.PrettyPrintType(value, out _));
        return value;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> DebugPrint<T>(
        IInvocationContext ctx,
        [PipedArgument] IEnumerable<T> value
    )
    {
        var list = value.ToList();

        ctx.WriteLine(Toolshed.PrettyPrintType(list, out _));
        return list;
    }

}
