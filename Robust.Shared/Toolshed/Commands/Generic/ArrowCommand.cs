using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Generic ;

[ToolshedCommand(Name = "=>")]
internal sealed class ArrowCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Arrow<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] T input,
        [CommandArgument] VarRef<T> @ref
        )
    {
        @ref.Set(ctx, input);
        return input;
    }
}
