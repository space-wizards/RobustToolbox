using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Math;

[ToolshedCommand]
public sealed class JoinCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public string Join(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] string x,
        [CommandArgument] ValueRef<string> y
    )
    {
        var yVal = y.Evaluate(ctx);
        if (yVal is null)
            return x;

        return x + y;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Join<T>(
            [CommandInvocationContext] IInvocationContext ctx,
            [PipedArgument] IEnumerable<T> x,
            [CommandArgument] ValueRef<IEnumerable<T>> y
        )
    {
        var yVal = y.Evaluate(ctx);
        if (yVal is null)
            return x;

        return x.Concat(yVal);
    }
}

[ToolshedCommand]
public sealed class AppendCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Append<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<T> x,
        [CommandArgument] ValueRef<T> y
    )
    {
        var yVal = y.Evaluate(ctx);
        if (yVal is null)
            return x;

        return x.Append(yVal);
    }
}
