using System;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Console;
using Robust.Shared.RTShell.Errors;
using Robust.Shared.RTShell.Syntax;

namespace Robust.Shared.RTShell.TypeParsers;

public sealed class ExpressionTypeParser : TypeParser<CommandRun>
{
    public override bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var res = CommandRun.TryParse(parser, null, null, false, out var r, out _, out error);
        result = r;
        return res;
    }

    public override bool TryAutocomplete(ForwardParser parser, string? argName, [NotNullWhen(true)] out CompletionResult? options, out IConError? error)
    {
        throw new NotImplementedException();
    }
}

public sealed class ExpressionTypeParser<T> : TypeParser<CommandRun<T>>
{
    public override bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var res = CommandRun<T>.TryParse(parser, null, false, out var r, out _, out error);
        result = r;
        return res;
    }

    public override bool TryAutocomplete(ForwardParser parser, string? argName, [NotNullWhen(true)] out CompletionResult? options, out IConError? error)
    {
        throw new NotImplementedException();
    }
}
