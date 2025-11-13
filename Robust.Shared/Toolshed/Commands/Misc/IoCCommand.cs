using System;
using System.Collections.Generic;
using Robust.Shared.IoC;

namespace Robust.Shared.Toolshed.Commands.Misc;

[ToolshedCommand]
internal sealed class IoCCommand : ToolshedCommand
{
    [Dependency] private readonly IDependencyCollection _deps = default!;
    [CommandImplementation("registered")]
    public IEnumerable<Type> Registered() => _deps.GetRegisteredTypes();

    [CommandImplementation("get")]
    public object? Get([PipedArgument] Type t) => _deps.ResolveType(t);
}
