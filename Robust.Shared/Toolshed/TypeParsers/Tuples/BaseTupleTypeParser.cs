using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Robust.Shared.Console;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.TypeParsers.Tuples;

public abstract class BaseTupleTypeParser<TParses> : TypeParser<TParses>
    where TParses: ITuple
{
    public abstract IEnumerable<Type> Fields { get; }

    public abstract TParses Create(IReadOnlyList<object> values);

    public override bool TryParse(ParserContext parserContext, [NotNullWhen(true)] out TParses? result)
    {
        var values = new List<object>();
        foreach (var field in Fields)
        {
            if (!Toolshed.TryParse(parserContext, field, out var parsed))
            {
                result = default;
                return false;
            }

            values.Add(parsed);
        }

        result = Create(values);
        return true;
    }

    public override CompletionResult? TryAutocomplete(ParserContext parserContext, CommandArgument? arg)
    {
        foreach (var field in Fields)
        {
            var checkpoint = parserContext.Save();
            if (Toolshed.TryParse(parserContext, field, out _) &&
                Rune.IsWhiteSpace(parserContext.PeekRune() ?? new Rune('.')))
                continue;

            parserContext.Restore(checkpoint);
            return Toolshed.TryAutocomplete(parserContext, field, null);
        }

        return null;
    }
}
