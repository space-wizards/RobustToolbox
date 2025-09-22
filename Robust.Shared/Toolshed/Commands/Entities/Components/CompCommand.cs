using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Toolshed.TypeParsers;

namespace Robust.Shared.Toolshed.Commands.Entities.Components;

[ToolshedCommand]
internal sealed class CompCommand : ToolshedCommand
{
    private static Type[] _parsers = [typeof(ComponentTypeParser)];
    public override Type[] TypeParameterParsers => _parsers;

    [CommandImplementation("get")]
    public IEnumerable<T> CompEnumerable<T>([PipedArgument] IEnumerable<EntityUid> input)
        where T: IComponent
    {
        return input.Where(HasComp<T>).Select(Comp<T>);
    }

    [CommandImplementation("get")]
    public T? CompDirect<T>([PipedArgument] EntityUid input)
        where T : IComponent
    {
        TryComp(input, out T? res);
        return res;
    }

    [CommandImplementation("add")]
    public EntityUid Add<T>([PipedArgument] EntityUid input)
        where T: IComponent, new()
    {
        AddComp<T>(input);
        return input;
    }

    [CommandImplementation("add")]
    public IEnumerable<EntityUid> Add<T>([PipedArgument] IEnumerable<EntityUid> input)
        where T : IComponent, new()
        => input.Select(Add<T>);


    [CommandImplementation("rm")]
    public EntityUid Rm<T>([PipedArgument] EntityUid input)
        where T: IComponent, new()
    {
        RemComp<T>(input);
        return input;
    }

    [CommandImplementation("rm")]
    public IEnumerable<EntityUid> Rm<T>([PipedArgument] IEnumerable<EntityUid> input)
        where T : IComponent, new()
        => input.Select(Rm<T>);

    [CommandImplementation("ensure")]
    public EntityUid Ensure<T>([PipedArgument] EntityUid input)
        where T: IComponent, new()
    {
        EnsureComp<T>(input);
        return input;
    }

    [CommandImplementation("ensure")]
    public IEnumerable<EntityUid> Ensure<T>([PipedArgument] IEnumerable<EntityUid> input)
        where T : IComponent, new()
        => input.Select(Ensure<T>);

    [CommandImplementation("has")]
    public bool Has<T>([PipedArgument] EntityUid input)
        where T: IComponent
    {
        return HasComp<T>(input);
    }

    [CommandImplementation("has")]
    public IEnumerable<bool> Has<T>([PipedArgument] IEnumerable<EntityUid> input)
        where T : IComponent
        => input.Select(Has<T>);
}
