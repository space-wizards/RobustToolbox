using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers.Math;

internal sealed class NumberBaseTypeParser<T> : TypeParser<T>
    where T: INumberBase<T>
{
    public override bool TryParse(ParserContext parserContext, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var maybeNumber = parserContext.GetWord(ParserContext.IsNumeric);
        if (maybeNumber?.Length == 0)
        {
            error = new OutOfInputError();
            result = null;
            return false;
        }

        if (!T.TryParse(maybeNumber, NumberStyles.Number, CultureInfo.InvariantCulture, out var @number))
        {
            if (maybeNumber is null)
            {
                error = new OutOfInputError();
            }
            else
            {
                error = new InvalidNumber<T>(maybeNumber);
            }

            result = null;
            return false;
        }
        result = @number;
        error = null;
        return true;
    }

    public override ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ParserContext parserContext,
        string? argName)
    {
        return new ValueTask<(CompletionResult? result, IConError? error)>(
                (CompletionResult.FromHint(typeof(T).PrettyName()), null)
            );
    }
}

public record InvalidNumber<T>(string Value) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkup(
            $"The value {Value} is not a valid {typeof(T).PrettyName()}.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
