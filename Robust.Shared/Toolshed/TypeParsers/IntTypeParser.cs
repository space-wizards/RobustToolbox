using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;

internal sealed class IntTypeParser : TypeParser<int>
{
    public override bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var maybeInt = parser.GetWord();
        if (!int.TryParse(maybeInt, out var @int))
        {
            if (maybeInt is null)
            {
                error = new OutOfInputError();
            }
            else
            {
                error = new InvalidInteger(maybeInt);
            }

            result = null;
            return false;
        }

        result = @int;
        error = null;
        return true;
    }

    public override ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ForwardParser parser,
        string? argName)
    {
        return new ValueTask<(CompletionResult? result, IConError? error)>(
                (CompletionResult.FromHint($"integer between {int.MinValue} and {int.MaxValue}"), null)
            );
    }
}

public record struct InvalidInteger(string Value) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkup(
            $"The value {Value} is not a valid integer. Please input some integer value between {int.MinValue} and {int.MaxValue}.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}

