using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Robust.Shared.Console;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;

internal sealed class StringTypeParser : TypeParser<string>
{
    // Completion option for hinting that all strings must start with a quote
    private static readonly CompletionOption[] Option = [new("\"", Flags: CompletionOptionFlags.PartialCompletion | CompletionOptionFlags.NoEscape)];

    public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out string? result)
    {
        ctx.ConsumeWhitespace();
        if (!ctx.EatMatch('"'))
        {
            if (ctx.PeekRune() is null)
            {
                ctx.Error = new OutOfInputError();
                result = null;
                return false;
            }

            ctx.Error = new StringMustStartWithQuote();
            ctx.Error.Contextualize(ctx.Input, (ctx.Index, ctx.Index + 1));
            result = null;
            return false;
        }

        var output = new StringBuilder();
        while (ctx.GetRune() is {} r)
        {
            if (r == new Rune('"'))
            {
                result = output.ToString();
                return true;
            }

            if (r != new Rune('\\'))
            {
                output.Append(r);
                continue;
            }

            var escaped = ctx.GetRune();
            if (escaped is null)
                continue;

            if (r == new Rune('"') || r == new Rune('n') || r == new Rune('\\'))
            {
                output.Append(escaped);
                continue;
            }

            ctx.Error = new UnknownEscapeSequence(escaped.Value);
            result = null;
            return false;
        }

        // We ran out of input before encountering the terminating quote.
        // Either someone is trying to execute an incomplete command, or more likely, they forgot the terminating quote.
        if (!ctx.GenerateCompletions)
            ctx.Error = new StringMustEndWithQuote();
        result = null;
        return false;
    }

    public override CompletionResult? TryAutocomplete(ParserContext parserContext, CommandArgument? arg)
    {
        var hint = GetArgHint(arg);
        parserContext.ConsumeWhitespace();
        return parserContext.PeekRune() == new Rune('"')
            ? CompletionResult.FromHint(hint)
            : CompletionResult.FromHintOptions(Option, hint);
    }
}

public record struct StringMustStartWithQuote : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted("A string must start with a quote (\").");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}

public sealed class StringMustEndWithQuote : ConError
{
    public override FormattedMessage DescribeInner()
        => FormattedMessage.FromUnformatted($"String must end with a quote (\").");
}

public sealed class UnknownEscapeSequence(Rune c) : ConError
{
    public override FormattedMessage DescribeInner()
        => FormattedMessage.FromUnformatted($"Unknown escape sequence: \\{c}");
}
