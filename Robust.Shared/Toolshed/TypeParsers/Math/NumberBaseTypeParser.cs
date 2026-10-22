using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using Robust.Shared.Console;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers.Math;

internal sealed class NumberBaseTypeParser<T> : TypeParser<T>
    where T: INumberBase<T>
{
    public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out T? result)
    {
        result = default;
        var maybeNumber = ctx.GetWord(ParserContext.IsNumeric);
        if (string.IsNullOrEmpty(maybeNumber))
        {
            ctx.Error = new ExpectedNumericError();
            return false;
        }

        if (T.TryParse(maybeNumber, NumberStyles.Number, CultureInfo.InvariantCulture, out result))
            return true;

        ctx.Error = new InvalidNumber<T>(maybeNumber);
        return false;
    }

    public override CompletionResult? TryAutocomplete(
        ParserContext parserContext,
        CommandArgument? arg)
    {
        return CompletionResult.FromHint(GetArgHint(arg));
    }
}

public sealed class ExpectedNumericError : ConError
{
    public override FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted($"Expected a number");
    }
}

public record InvalidNumber<T>(string Value) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted(
            $"The value {Value} is not a valid {typeof(T).PrettyName()}.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
