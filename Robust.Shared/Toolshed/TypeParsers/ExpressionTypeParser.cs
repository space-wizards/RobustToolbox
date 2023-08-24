using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.TypeParsers;

internal sealed class ExpressionTypeParser : TypeParser<CommandRun>
{
    public override bool TryParse(ParserContext parserContext, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var res = CommandRun.TryParse(false, parserContext, null, null, false, out var r, out _, out error);
        result = r;
        return res;
    }

    public override ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ParserContext parserContext,
        string? argName)
    {
        CommandRun.TryParse(true, parserContext, null, null, false, out _, out var autocomplete, out _);
        if (autocomplete is null)
            return ValueTask.FromResult<(CompletionResult? result, IConError? error)>((null, null));

        return autocomplete.Value;
    }

}

internal sealed class ExpressionTypeParser<T> : TypeParser<CommandRun<T>>
{
    public override bool TryParse(ParserContext parserContext, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var res = CommandRun<T>.TryParse(false, false, parserContext, null, false, out var r, out _, out error);
        result = r;
        return res;
    }

    public override ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ParserContext parserContext,
        string? argName)
    {
        CommandRun<T>.TryParse(false, true, parserContext, null, false, out _, out var autocomplete, out _);
        if (autocomplete is null)
            return ValueTask.FromResult<(CompletionResult? result, IConError? error)>((null, null));

        return autocomplete.Value;
    }
}

internal sealed class ExpressionTypeParser<TIn, TOut> : TypeParser<CommandRun<TIn, TOut>>
{
    public override bool TryParse(ParserContext parserContext, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var res = CommandRun<TIn, TOut>.TryParse(false, false, parserContext, false, out var r, out _, out error);
        result = r;
        return res;
    }

    public override ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ParserContext parserContext,
        string? argName)
    {
        CommandRun<TIn, TOut>.TryParse(false, true, parserContext, false, out _, out var autocomplete, out _);
        if (autocomplete is null)
            return ValueTask.FromResult<(CompletionResult? result, IConError? error)>((null, null));

        return autocomplete.Value;
    }
}
