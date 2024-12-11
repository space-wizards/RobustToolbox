using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;

namespace Robust.Shared.Toolshed.Commands.Generic.Ordering;

[ToolshedCommand]
public sealed class SortByCommand : ToolshedCommand
{
    private static Type[] _parsers = [typeof(MapBlockOutputParser)];
    public override Type[] TypeParameterParsers => _parsers;

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> SortBy<TOrd, T>(
        IInvocationContext ctx,
        [PipedArgument] IEnumerable<T> input,
        Block<T, TOrd> orderer
    )
        where TOrd : IComparable<TOrd>
        => input.OrderBy(x => orderer.Invoke(x, ctx)!);
}
