using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Console;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.TypeParsers;

public sealed class ArrayTypeParser<T> : TypeParser<T[]>
{
    public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out T[]? result)
    {
        result = null;
        if (!Toolshed.TryParse(ctx, out List<T>? list))
            return false;

        result = list.ToArray();
        return true;
    }

    public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        return Toolshed.TryAutocomplete(ctx, typeof(List<T>), arg);
    }
}
