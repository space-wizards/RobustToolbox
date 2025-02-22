using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Generic.Variables ;

[ToolshedCommand(Name = "=>")]
public sealed class ArrowCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Arrow<T>(IInvocationContext ctx, [PipedArgument] T input, WriteableVarRef<T> var)
    {
        ctx.WriteVar(var.Inner.VarName, input);
        return input;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public List<T> Arrow<T>(IInvocationContext ctx, [PipedArgument] IEnumerable<T> input, WriteableVarRef<List<T>> var)
    {
        var list = input.ToList();
        ctx.WriteVar(var.Inner.VarName, list);
        return list;
    }
}
