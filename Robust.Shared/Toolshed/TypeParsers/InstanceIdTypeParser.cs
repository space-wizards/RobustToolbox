using System;
using Robust.Shared.Console;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.TypeParsers;

public sealed class InstanceIdTypeParser : TypeParser<InstanceId>
{
    public override bool TryParse(ParserContext parserContext, out InstanceId result)
    {
        result = new InstanceId(Guid.NewGuid());
        return true;
    }

    public override CompletionResult? TryAutocomplete(ParserContext parserContext, CommandArgument? arg)
    {
        return CompletionResult.FromHint(GetArgHint(arg));
    }
}

public record struct InstanceId(Guid Id);
