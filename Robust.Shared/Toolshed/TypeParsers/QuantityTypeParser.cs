using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;

internal sealed class QuantityParser : TypeParser<Quantity>
{
    public override bool TryParse(ParserContext parserContext, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var word = parserContext.GetWord(ParserContext.IsNumeric);
        error = null;

        if (word?.TrimEnd('%') is not { } maybeParseable || !float.TryParse(maybeParseable, out var v))
        {
            if (word is not null)
                error = new InvalidQuantity(word);
            else
                error = new OutOfInputError();

            result = null;
            return false;
        }

        if (v < 0.0)
        {
            error = new InvalidQuantity(word);
            result = null;
            return false;
        }

        if (word.EndsWith('%'))
        {
            if (v > 100.0)
            {
                error = new InvalidQuantity(word);
                result = null;
                return false;
            }

            result = new Quantity(null, (v / 100.0f));
            return true;
        }

        result = new Quantity(v, null);
        return true;
    }

    public override ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ParserContext parserContext,
        string? argName)
    {
        return ValueTask.FromResult<(CompletionResult? result, IConError? error)>((CompletionResult.FromHint($"{argName ?? "quantity"}"), null));
    }
}

public readonly record struct Quantity(float? Amount, float? Percentage);

public record InvalidQuantity(string Value) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkup(
            $"The value {Value} is not a valid quantity. Please input some decimal number, optionally with a % to indicate that it's a percentage.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
