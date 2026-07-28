using System;
using Content.Shared.Toolshed;
using Robust.Shared.Toolshed.TypeParsers;

namespace Robust.Shared.Toolshed.Commands.Values;

[ToolshedCommand]
public sealed class ValArrCommand : ToolshedCommand
{
    private static Type[] _parsers = [typeof(TypeTypeParser)];
    public override Type[] TypeParameterParsers => _parsers;

    [CommandImplementation]
    public ValueArray<T> ValArr<T>(ValueArray<T> array) => array;
}
