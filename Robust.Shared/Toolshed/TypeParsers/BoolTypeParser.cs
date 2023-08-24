using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;

public sealed class BoolTypeParser : TypeParser<bool>
{
    public override bool TryParse(ParserContext parserContext, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var word = parserContext.GetWord(ParserContext.IsToken)?.ToLowerInvariant();
        if (word is null)
        {
            if (parserContext.PeekChar() is null)
            {
                error = new OutOfInputError();
                result = null;
                return false;
            }
            else
            {
                error = new InvalidBool(parserContext.GetWord()!);
                result = null;
                return false;
            }
        }

        if (word == "true" || word == "t" || word == "1")
        {
            result = true;
            error = null;
            return true;
        } else if (word == "false" || word == "f" || word == "0")
        {
            result = false;
            error = null;
            return true;
        }
        else
        {
            error = new InvalidBool(word);
            result = null;
            return false;
        }
    }

    public override async ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ParserContext parserContext, string? argName)
    {
        return (CompletionResult.FromOptions(new[] {"true", "false"}), null);
    }
}


public record InvalidBool(string Value) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkup(
            $"The value {Value} is not a valid boolean.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
