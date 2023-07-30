using System;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Generic;


[ToolshedCommand]
internal sealed class BlockCommand : ToolshedCommand
{
    public override Type[] TypeParameterParsers => new[] {typeof(Type)};

    [CommandImplementation]
    public Block<T> Block<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [CommandArgument] Block<T> value
    ) => value;
}
