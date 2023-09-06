using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;

internal sealed class ResPathTypeParser : StringTypeParser
{
    public override Type Parses => typeof(ResPath);

    public override bool TryParse(ParserContext parserContext, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var baseResult = base.TryParse(parserContext, out result, out error);

        if (!baseResult)
            return false;

        result = new ResPath((string) result!);
        return true;
    }

    public override ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ParserContext parserContext,
        string? argName)
    {
        return base.TryAutocomplete(parserContext, argName);
    }
}
