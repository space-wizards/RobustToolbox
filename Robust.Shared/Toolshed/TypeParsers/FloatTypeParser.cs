using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;

internal sealed class FloatTypeParser : TypeParser<float>
{
    public override bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var maybeFloat = parser.GetWord();
        if (!float.TryParse(maybeFloat, out var @float))
        {
            if (maybeFloat is null)
            {
                error = new OutOfInputError();
            }
            else
            {
                error = new InvalidInteger(maybeFloat);
            }

            result = null;
            return false;
        }

        result = @float;
        error = null;
        return true;
    }

    public override ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ForwardParser parser,
        string? argName)
    {
        return new ValueTask<(CompletionResult? result, IConError? error)>(
                (CompletionResult.FromHint($"any float (decimal number)"), null)
            );
    }
}

public record struct InvalidFloat(string Value) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkup(
            $"The value {Value} is not a valid floating point number. Only decimal numbers are accepted.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}

