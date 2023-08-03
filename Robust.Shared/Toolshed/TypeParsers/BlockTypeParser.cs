using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.TypeParsers;

internal sealed class BlockTypeParser : TypeParser<Block>
{
    public BlockTypeParser()
    {
    }

    public override bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var r = Block.TryParse(false, parser, null, out var block, out _, out error);
        result = block;
        return r;
    }

    public override ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ForwardParser parser,
        string? argName)
    {
        Block.TryParse(true, parser, null, out _, out var autocomplete, out _);
        if (autocomplete is null)
            return ValueTask.FromResult<(CompletionResult? result, IConError? error)>((null, null));

        return autocomplete.Value;
    }
}


internal sealed class BlockTypeParser<T> : TypeParser<Block<T>>
{
    public BlockTypeParser()
    {
    }

    public override bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var r = Block<T>.TryParse(false, parser, null, out var block, out _, out error);
        result = block;
        return r;
    }

    public override ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ForwardParser parser,
        string? argName)
    {
        Block<T>.TryParse(true, parser, null, out _, out var autocomplete, out _);
        if (autocomplete is null)
            return ValueTask.FromResult<(CompletionResult? result, IConError? error)>((null, null));

        return autocomplete.Value;
    }
}

internal sealed class BlockTypeParser<TIn, TOut> : TypeParser<Block<TIn, TOut>>
{
    public BlockTypeParser()
    {
    }

    public override bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var r = Block<TIn, TOut>.TryParse(false, parser, null, out var block, out _, out error);
        result = block;
        return r;
    }

    public override ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ForwardParser parser,
        string? argName)
    {
        Block<TIn, TOut>.TryParse(true, parser, null, out _, out var autocomplete, out _);
        if (autocomplete is null)
            return ValueTask.FromResult<(CompletionResult? result, IConError? error)>((null, null));

        return autocomplete.Value;
    }
}
