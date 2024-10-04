using System;
using Robust.Shared.Toolshed.TypeParsers;

namespace Robust.Shared.Toolshed.Commands.Generic;

[ToolshedCommand]
public sealed class AsCommand : ToolshedCommand
{
    private static Type[] _parsers = [typeof(TypeTypeParser)];
    public override Type[] TypeParameterParsers => _parsers;

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public TOut? As<TOut, TIn>([PipedArgument] TIn value)
        => (TOut?)(object?)value;
}
