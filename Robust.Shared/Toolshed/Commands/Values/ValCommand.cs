using System;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Values;

[ToolshedCommand]
public sealed class ValCommand : ToolshedCommand
{
    public override Type[] TypeParameterParsers => new[] {typeof(Type)};

    [CommandImplementation]
    public T Val<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [CommandArgument] ValueRef<T> value
        ) => value.Evaluate(ctx)!;
}
