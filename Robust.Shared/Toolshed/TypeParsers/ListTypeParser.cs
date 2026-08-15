using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Robust.Shared.Console;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers.Math;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;

public sealed class ListTypeParser<T> : TypeParser<List<T>>
{
    public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out List<T>? result)
    {
        ctx.ConsumeWhitespace();
        result = null;

        if (!ctx.EatMatch('['))
        {
            ctx.Error = new ExpectedOpenBrace();
            return false;
        }

        var values = new List<T>();

        var (minLength, maxLength) = GetLengthParameters(ctx.CurrentArgument);

        while (true)
        {
            ctx.ConsumeWhitespace();

            if (!Toolshed.TryParse(ctx, out T? value))
                return false;

            values.Add(value);

            if (maxLength >= 0 && values.Count > maxLength)
            {
                ctx.Error = new TooManyElementsError(maxLength);
                return false;
            }

            ctx.ConsumeWhitespace();

            if (ctx.EatMatch(','))
                continue;

            if (ctx.EatMatch(']'))
            {
                if (values.Count < minLength)
                {
                    ctx.Error = new NotEnoughElementsError(minLength);
                    return false;
                }

                result = new List<T>(values.ToArray());
                return true;
            }

            ctx.Error = new ExpectedTokenError([",", "]"]);
            return false;
        }
    }

    public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        var hint = GetArgHint(arg);

        ctx.ConsumeWhitespace();

        if (!ctx.EatMatch('['))
        {
            return CompletionResult.FromHintOptions([
                    new CompletionOption("[",
                        Flags: CompletionOptionFlags.PartialCompletion | CompletionOptionFlags.NoEscape |
                               CompletionOptionFlags.AppendOnly)
                ],
                hint);
        }

        var (minLength, maxLength) = GetLengthParameters(arg);
        int count = 0;

        while (true)
        {
            ctx.ConsumeWhitespace();

            if (ctx.PeekRune() ==
                new Rune(']')) // this doesn't show in autocomplete, but I can't be bothered to touch anything below here again with a 20000000ft pole.
                return CompletionResult.FromHint(hint);

            var restore = ctx.Save();

            if (!Toolshed.TryParse(ctx, out T? _))
            {
                ctx.Restore(restore);
                var result = Toolshed.TryAutocomplete(ctx, typeof(T), arg);
                if (result is null) return result;
                List<CompletionOption> opts = [];
                opts.AddRange(result.Options.Select(opt =>
                    new CompletionOption(opt.Value,
                        opt.Hint,
                        opt.Flags | CompletionOptionFlags.IgnoreCurrent | CompletionOptionFlags.AppendOnly)));
                return new CompletionResult(opts.ToArray(), result.Hint);
            }

            ctx.ConsumeWhitespace();
            count++;

            if (ctx.PeekRune() is null)
            {
                List<CompletionOption> opts = [];

                if (maxLength < 0 || maxLength > count)
                {
                    opts.Add(new CompletionOption(",",
                        Flags: CompletionOptionFlags.NoEscape | CompletionOptionFlags.IgnoreCurrent |
                               CompletionOptionFlags.AppendOnly));
                }

                if (count >= minLength || count >= maxLength)
                {
                    opts.Add(new CompletionOption("]",
                        Flags: CompletionOptionFlags.NoEscape | CompletionOptionFlags.IgnoreCurrent |
                               CompletionOptionFlags.AppendOnly));
                }

                return CompletionResult.FromHintOptions(opts, hint);
            }

            if (ctx.EatMatch(','))
                continue;

            if (ctx.EatMatch(']'))
                return CompletionResult.FromHint(hint);

            return CompletionResult.FromHintOptions([
                    new CompletionOption("]",
                        Flags: CompletionOptionFlags.NoEscape | CompletionOptionFlags.IgnoreCurrent |
                               CompletionOptionFlags.AppendOnly)
                ],
                hint);
        }
    }

    private (int minLength, int maxLength) GetLengthParameters(CommandArgument? arg)
    {
        return (
            arg?.ListLengthAttribute?.MinLength ?? 0,
            arg?.ListLengthAttribute?.MaxLength ?? -1
        );
    }
}

public sealed class ExpectedTokenError(string[] expectedTokens) : ConError
{
    public override FormattedMessage DescribeInner() =>
        FormattedMessage.FromUnformatted($"Expected one of the following tokens: {string.Join(", ", expectedTokens)}");
}

public sealed class TooManyElementsError(int max) : ConError
{
    public override FormattedMessage DescribeInner() =>
        FormattedMessage.FromUnformatted($"Too many elements, maximum length is {max}.");
}

public sealed class NotEnoughElementsError(int min) : ConError
{
    public override FormattedMessage DescribeInner() =>
        FormattedMessage.FromUnformatted($"Not enough elements, minimum length is {min}.");
}
