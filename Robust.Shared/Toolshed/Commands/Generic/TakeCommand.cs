using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Generic;

[ToolshedCommand]
public sealed class TakeCommand : ToolshedCommand
{
    [CommandImplementation]
    public IEnumerable<T> Take<T>(
            [CommandInvocationContext] IInvocationContext ctx,
            [PipedArgument] IEnumerable<T> input,
            [CommandArgument] ValueRef<int> amount
        )
        => input.Take(amount.Evaluate(ctx));
}
