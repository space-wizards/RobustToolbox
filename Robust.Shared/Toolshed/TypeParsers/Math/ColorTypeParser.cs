using System.Diagnostics;
using System.Linq;
using System.Text;
using Robust.Shared.Console;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers.Math;

public sealed class ColorTypeParser : TypeParser<Color>
{
    public override bool TryParse(ParserContext ctx, out Color result)
    {
        result = default;
        var word = ctx.GetWord(x => ParserContext.IsToken(x) || x == new Rune('#'))?.ToLowerInvariant();
        if (word is null)
        {
            if (ctx.PeekRune() is null)
            {
                ctx.Error = new OutOfInputError();
                return false;
            }

            ctx.Error = new InvalidColor(ctx.GetWord()!);
            result = default;
            return false;
        }

        if (Color.TryParse(word, out result))
            return true;

        ctx.Error = new InvalidColor(word);
        return false;

    }

    public override CompletionResult TryAutocomplete(ParserContext parserContext, CommandArgument? arg)
    {
        return CompletionResult.FromHintOptions(Color.GetAllDefaultColors().Select(x => x.Key),
            $"{GetArgHint(arg)}\nHex code or color name.");
    }
}

public record InvalidColor(string Value) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted(
            $"The value {Value} is not a valid RGB color or name of color.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
