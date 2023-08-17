using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Generic.Ordering;

[ToolshedCommand, MapLikeCommand]
public sealed class SortByCommand : ToolshedCommand
{
    public override Type[] TypeParameterParsers => new[] {typeof(Type)};

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> SortBy<TOrd, T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<T> input,
        [CommandArgument] Block<T, TOrd> orderer
    )
        where TOrd : IComparable<TOrd>
        => input.OrderBy(x => orderer.Invoke(x, ctx)!);
}
