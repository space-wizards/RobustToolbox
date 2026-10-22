using System;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;

namespace Robust.Shared.Toolshed.Commands.Values;

[ToolshedCommand]
public sealed class ValCommand : ToolshedCommand
{
    private static Type[] _parsers = [typeof(TypeTypeParser)];
    public override Type[] TypeParameterParsers => _parsers;

    [CommandImplementation]
    public T Val<T>(T value) => value;
}
