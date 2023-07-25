using System;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Console;
using Robust.Shared.RTShell.Errors;
using Robust.Shared.RTShell.Syntax;

namespace Robust.Shared.RTShell.TypeParsers;

public sealed class BlockTypeParser<T> : TypeParser<Block<T>>
{
    public override bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var r = Block<T>.TryParse(parser, null, out var block, out error);
        result = block;
        return r;
    }

    public override bool TryAutocomplete(ForwardParser parser, string? argName, [NotNullWhen(true)] out CompletionResult? options, out IConError? error)
    {
        throw new NotImplementedException();
    }
}

public sealed class BlockTypeParser<TIn, TOut> : TypeParser<Block<TIn, TOut>>
{
    public override bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var r = Block<TIn, TOut>.TryParse(parser, null, out var block, out error);
        result = block;
        return r;
    }

    public override bool TryAutocomplete(ForwardParser parser, string? argName, [NotNullWhen(true)] out CompletionResult? options, out IConError? error)
    {
        throw new NotImplementedException();
    }
}
