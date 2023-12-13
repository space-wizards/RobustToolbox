using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;

[Virtual]
internal class StringTypeParser : TypeParser<string>
{
    public override bool TryParse(ParserContext parserContext, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        error = null;
        parserContext.ConsumeWhitespace();
        if (parserContext.PeekRune() != new Rune('"'))
        {
            if (parserContext.PeekRune() is null)
            {
                error = new OutOfInputError();
                result = null;
                return false;
            }

            error = new StringMustStartWithQuote();
            error.Contextualize(parserContext.Input, (parserContext.Index, parserContext.Index + 1));
            result = null;
            return false;
        }

        parserContext.GetRune();

        var output = new StringBuilder();

        while (true)
        {
            while (parserContext.PeekChar() is not '"' and not '\\' and not null)
            {
                output.Append(parserContext.GetRune());
            }

            if (parserContext.PeekChar() is '"' or null)
            {
                if (parserContext.PeekRune() is null)
                {
                    error = new OutOfInputError();
                    result = null;
                    return false;
                }

                parserContext.GetRune();
                break;
            }

            parserContext.GetRune(); // okay it's \

            switch (parserContext.GetChar())
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

        parserContext.ConsumeWhitespace();

        result = output.ToString();
        return true;
    }

    public override ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ParserContext parserContext,
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
