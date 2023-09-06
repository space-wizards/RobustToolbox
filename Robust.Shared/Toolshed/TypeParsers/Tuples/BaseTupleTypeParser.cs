using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.TypeParsers.Tuples;

public abstract class BaseTupleTypeParser<TParses> : TypeParser<TParses>
    where TParses: ITuple
{
    [IoC.Dependency] private readonly ToolshedManager _toolshed = default!;

    public abstract IEnumerable<Type> Fields { get; }

    public abstract TParses Create(IReadOnlyList<object> values);

    public override bool TryParse(ParserContext parserContext, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var values = new List<object>();
        foreach (var field in Fields)
        {
            if (!_toolshed.TryParse(parserContext, field, out var parsed, out error))
            {
                result = null;
                return false;
            }

            values.Add(parsed);
        }

        result = Create(values);
        error = null;
        return true;
    }

    public override ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ParserContext parserContext, string? argName)
    {
        IConError? error = null;
        foreach (var field in Fields)
        {
            var checkpoint = parserContext.Save();
            if (!_toolshed.TryParse(parserContext, field, out _, out error) || !Rune.IsWhiteSpace(parserContext.PeekRune() ?? new Rune('.')))
            {
                parserContext.Restore(checkpoint);
                return _toolshed.TryAutocomplete(parserContext, field, argName);
            }
        }

        return new ValueTask<(CompletionResult? result, IConError? error)>((null, null));
    }
}
