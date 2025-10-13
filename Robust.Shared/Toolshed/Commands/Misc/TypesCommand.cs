using System;
using System.Collections.Generic;

namespace Robust.Shared.Toolshed.Commands.Misc;

[ToolshedCommand]
internal sealed class TypesCommand : ToolshedCommand
{
    [CommandImplementation("tree")]
    public IEnumerable<Type> Tree(IInvocationContext ctx, [PipedArgument] object? input)
    {
        var t = input is Type ? (Type)input : input!.GetType();
        return Toolshed.AllSteppedTypes(t);
    }

    [CommandImplementation("gettype")]
    public Type GetType([PipedArgument] object? input)
    {
        return input?.GetType() ?? typeof(void);
    }

    [CommandImplementation("fullname")]
    public string FullName([PipedArgument] Type input)
    {
        return input.FullName!;
    }
}
