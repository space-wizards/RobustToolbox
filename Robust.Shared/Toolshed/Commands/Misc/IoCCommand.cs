using System;
using System.Collections.Generic;
using Robust.Shared.IoC;

namespace Robust.Shared.Toolshed.Commands.Misc;

[ToolshedCommand]
internal sealed class IoCCommand : ToolshedCommand
{
    [CommandImplementation("registered")]
    public IEnumerable<Type> Registered() => IoCManager.Instance!.GetRegisteredTypes();

    [CommandImplementation("get")]
    public object? Get([PipedArgument] Type t) => IoCManager.ResolveType(t);
}
