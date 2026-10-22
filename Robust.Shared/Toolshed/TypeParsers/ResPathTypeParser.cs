using Robust.Shared.Console;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;

internal sealed class ResPathTypeParser : TypeParser<ResPath>
{
    public override bool TryParse(ParserContext parserContext, out ResPath result)
    {
        result = default;
        if (!Toolshed.TryParse(parserContext, out string? str))
            return false;

        result = new ResPath(str);
        return true;
    }

    public override CompletionResult? TryAutocomplete(ParserContext parserContext, CommandArgument? arg)
    {
        // TODO TOOLSHED ResPath Completion
        return CompletionResult.FromHint(GetArgHint(arg));
    }
}
