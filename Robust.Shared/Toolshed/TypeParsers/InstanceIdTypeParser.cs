using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.TypeParsers;

public sealed class InstanceIdTypeParser : TypeParser<InstanceId>
{
    public override bool TryParse(ParserContext parserContext, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        result = new InstanceId(Guid.NewGuid());
        error = null;
        return true;
    }

    public override ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ParserContext parserContext, string? argName)
    {
        return new ValueTask<(CompletionResult? result, IConError? error)>((null, null));
    }
}

public record struct InstanceId(Guid Id);
