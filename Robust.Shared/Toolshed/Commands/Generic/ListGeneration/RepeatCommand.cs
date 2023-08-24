using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Generic.ListGeneration;

[ToolshedCommand(Name = "rep")]
public sealed class RepeatCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Repeat<T>(
            [CommandInvocationContext] IInvocationContext ctx,
            [PipedArgument] T value,
            [CommandArgument] ValueRef<int> amount
        )
        => Enumerable.Repeat(value, amount.Evaluate(ctx));
}
