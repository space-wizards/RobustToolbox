using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.TypeParsers.Math;

public abstract class SpanLikeTypeParser<T, TElem> : TypeParser<T>
    where T : notnull
    where TElem : unmanaged
{
    [Dependency] private readonly ToolshedManager _toolshed = default!;

    public abstract int Elements { get; }
    public abstract T Create(Span<TElem> elements);

    public override bool TryParse(ParserContext parserContext, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        Span<TElem> elements = stackalloc TElem[Elements];

        for (var i = 0; i < Elements; i++)
        {
            if (!_toolshed.TryParse<TElem>(parserContext, out var value, out error))
            {
                result = null;
                return false;
            }

            elements[i] = value;
        }

        error = null;
        result = Create(elements);
        return true;
    }

    public override ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ParserContext parserContext, string? argName)
    {
        return ValueTask.FromResult<(CompletionResult? result, IConError? error)>((CompletionResult.FromHint(typeof(T).PrettyName()), null));
    }
}
