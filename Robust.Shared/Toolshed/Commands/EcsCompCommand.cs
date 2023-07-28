using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Shared.Toolshed.Commands;

[ToolshedCommand]
internal sealed class EcsCompCommand : ToolshedCommand
{
    [Dependency] private readonly IComponentFactory _factory = default!;

    [CommandImplementation("listty")]
    public IEnumerable<Type> ListTy()
    {
        return _factory.AllRegisteredTypes;
    }
}
