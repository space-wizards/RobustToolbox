using System.Diagnostics;
using Robust.Shared.Console;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;

public sealed class BoolTypeParser : TypeParser<bool>
{
    public override bool TryParse(ParserContext ctx, out bool result)
    {
        var word = ctx.GetWord(ParserContext.IsToken)?.ToLowerInvariant();
        if (word is null)
        {
            if (ctx.PeekRune() is null)
            {
                ctx.Error = new OutOfInputError();
                result = default;
                return false;
            }

            ctx.Error = new InvalidBool(ctx.GetWord()!);
            result = default;
            return false;
        }

        if (word == "true" || word == "t" || word == "1")
        {
            result = true;
            return true;
        }

        if (word == "false" || word == "f" || word == "0")
        {
            result = false;
            return true;
        }

        ctx.Error = new InvalidBool(word);
        result = default;
        return false;
    }

    public override CompletionResult TryAutocomplete(ParserContext parserContext, CommandArgument? arg)
    {
        return CompletionResult.FromHintOptions(new[] {"true", "false"}, GetArgHint(arg));
    }
}


public record InvalidBool(string Value) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted(
            $"The value {Value} is not a valid boolean.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
