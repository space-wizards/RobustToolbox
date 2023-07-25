using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Toolshed.TypeParsers;

namespace Robust.Shared.Toolshed.Commands.Entities;

[ToolshedCommand]
internal sealed class CompCommand : ToolshedCommand
{
    public override Type[] TypeParameterParsers => new[] {typeof(ComponentType)};

    [CommandImplementation]
    public IEnumerable<T> CompEnumerable<T>([PipedArgument] IEnumerable<EntityUid> input)
        where T: IComponent
    {
        return input.Where(HasComp<T>).Select(Comp<T>);
    }

    [CommandImplementation]
    public T? CompDirect<T>([PipedArgument] EntityUid input)
        where T : IComponent
    {
        TryComp(input, out T? res);
        return res;
    }
}
