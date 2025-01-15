using System.Diagnostics;
using System.Globalization;
using Robust.Shared.Console;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers.Math;

public sealed class AngleTypeParser : TypeParser<Angle>
{
    public override bool TryParse(ParserContext ctx, out Angle result)
    {
        result = default;
        var word = ctx.GetWord(ParserContext.IsNumeric)?.ToLowerInvariant();
        if (word is null)
        {
            if (ctx.PeekRune() is null)
            {
                ctx.Error = new OutOfInputError();
                return false;
            }

            ctx.Error = new InvalidAngle(ctx.GetWord()!);
            return false;
        }

        if (word.EndsWith("deg"))
        {
            if (!float.TryParse(word[..^3], CultureInfo.InvariantCulture, out var f))
            {
                ctx.Error = new InvalidAngle(word);
                return false;
            }

            result = Angle.FromDegrees(f);
            return true;
        }
        else
        {
            if (!float.TryParse(word, CultureInfo.InvariantCulture, out var f))
            {
                ctx.Error = new InvalidAngle(word);
                result = default;
                return false;
            }

            result = new Angle(f);
            return true;
        }
    }

    public override CompletionResult? TryAutocomplete(ParserContext parserContext, CommandArgument? arg)
    {
        return CompletionResult.FromHint($"{GetArgHint(arg)}\nAppend \"deg\" for degrees");
    }
}

public record InvalidAngle(string Value) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted(
            $"The value {Value} is not a valid angle.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
