using System;

namespace Robust.Shared.Toolshed.Commands.Generic;

[ToolshedCommand]
public sealed class AsCommand : ToolshedCommand
{
    public override Type[] TypeParameterParsers => new[] {typeof(Type)};

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public TOut? As<TOut, TIn>([PipedArgument] TIn value)
        => (TOut?)(object?)value;
}
