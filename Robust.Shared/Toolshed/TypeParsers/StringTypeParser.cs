using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;

[Virtual]
internal class StringTypeParser : TypeParser<string>
{
    public override bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        error = null;
        parser.Consume(char.IsWhiteSpace);
        if (parser.PeekChar() is not '"')
        {
            if (parser.PeekChar() is null)
            {
                error = new OutOfInputError();
                result = null;
                return false;
            }

            error = new StringMustStartWithQuote();
            error.Contextualize(parser.Input, (parser.Index, parser.Index + 1));
            result = null;
            return false;
        }

        parser.GetChar();

        var output = new StringBuilder();

        while (true)
        {
            while (parser.PeekChar() is not '"' and not '\\' and not null)
            {
                output.Append(parser.GetChar());
            }

            if (parser.PeekChar() is '"' or null)
            {
                if (parser.PeekChar() is null)
                {
                    error = new OutOfInputError();
                    result = null;
                    return false;
                }

                parser.GetChar();
                break;
            }

            parser.GetChar(); // okay it's \

            switch (parser.GetChar())
            {
                case '"':
                    output.Append('"');
                    continue;
                case 'n':
                    output.Append('\n');
                    continue;
                case '\\':
                    output.Append('\\');
                    continue;
                default:
                    result = null;
                    // todo: error
                    return false;
            }
        }

        parser.Consume(char.IsWhiteSpace);

        result = output.ToString();
        return true;
    }

    public override ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ForwardParser parser,
        string? argName)
    {
        return ValueTask.FromResult<(CompletionResult? result, IConError? error)>((CompletionResult.FromHint($"\"<{argName ?? "string"}>\""), null));
    }
}

public record struct StringMustStartWithQuote : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkup("A string must start with a quote.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
