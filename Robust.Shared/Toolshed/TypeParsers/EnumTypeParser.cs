using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Console;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;

public sealed class EnumTypeParser<T> : TypeParser<T>
    where T : unmanaged, Enum
{
    public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out T result)
    {
        var word = ctx.GetWord(ParserContext.IsToken);
        if (word is null)
        {
            if (ctx.PeekRune() is null)
            {
                ctx.Error = new OutOfInputError();
                result = default;
                return false;
            }

            ctx.Error = new InvalidEnum<T>(ctx.GetWord()!);
            result = default;
            return false;
        }

        if (!Enum.TryParse<T>(word, ignoreCase: true, out var value))
        {
            result = default;
            ctx.Error = new InvalidEnum<T>(word);
            return false;
        }

        result = value;
        return true;
    }

    public override CompletionResult? TryAutocomplete(ParserContext parserContext, CommandArgument? arg)
    {
        return CompletionResult.FromHintOptions(Enum.GetNames<T>(), GetArgHint(arg));
    }
}

public record InvalidEnum<T>(string Value) : IConError
    where T : unmanaged, Enum
{
    public FormattedMessage DescribeInner()
    {
        var msg = FormattedMessage.FromUnformatted($"The value {Value} is not a valid {typeof(T).PrettyName()}.");
        msg.AddText($"Valid values are: {string.Join(", ", Enum.GetNames<T>())}");
        return msg;
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
