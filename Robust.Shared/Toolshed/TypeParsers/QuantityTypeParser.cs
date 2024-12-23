using System.Diagnostics;
using Robust.Shared.Console;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;

internal sealed class QuantityParser : TypeParser<Quantity>
{
    public override bool TryParse(ParserContext ctx, out Quantity result)
    {
        result = default;
        var word = ctx.GetWord(ParserContext.IsNumeric);

        if (word?.TrimEnd('%') is not { } maybeParseable || !float.TryParse(maybeParseable, out var v))
        {
            ctx.Error = word is not null ? new InvalidQuantity(word) : new OutOfInputError();

            return false;
        }

        if (v < 0.0)
        {
            ctx.Error = new InvalidQuantity(word);
            return false;
        }

        if (word.EndsWith('%'))
        {
            if (v > 100.0)
            {
                ctx.Error = new InvalidQuantity(word);
                return false;
            }

            result = new Quantity(null, (v / 100.0f));
            return true;
        }

        result = new Quantity(v, null);
        return true;
    }

    public override CompletionResult? TryAutocomplete(
        ParserContext parserContext,
        CommandArgument? arg)
    {
        return CompletionResult.FromHint(GetArgHint(arg));
    }
}

public readonly record struct Quantity(float? Amount, float? Percentage);

public record InvalidQuantity(string Value) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted(
            $"The value {Value} is not a valid quantity. Please input some decimal number, optionally with a % to indicate that it's a percentage.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
