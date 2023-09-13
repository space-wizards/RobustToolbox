using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;

public sealed class EnumTypeParser<T> : TypeParser<T>
    where T: unmanaged, Enum
{
    public override bool TryParse(ParserContext parserContext, [NotNullWhen(true)] out object? result,
        out IConError? error)
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
                error = new InvalidEnum<T>(parserContext.GetWord()!);
                result = null;
                return false;

            }
        }

        if (!Enum.TryParse<T>(word, out var value))
        {
            result = null;
            error = new InvalidEnum<T>(word);
            return false;
        }

        result = value;
        error = null;
        return true;
    }

    public override async ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ParserContext parserContext, string? argName)
    {
        return (CompletionResult.FromOptions(Enum.GetNames<T>()), null);
    }
}

public record InvalidEnum<T>(string Value) : IConError
    where T: unmanaged, Enum
{
    public FormattedMessage DescribeInner()
    {
        var msg = FormattedMessage.FromMarkup($"The value {Value} is not a valid {typeof(T).PrettyName()}.");
        msg.AddText($"Valid values are: {string.Join(", ", Enum.GetNames<T>())}");
        return msg;
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
