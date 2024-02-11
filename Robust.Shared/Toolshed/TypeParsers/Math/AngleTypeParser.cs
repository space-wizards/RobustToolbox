using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers.Math;

public sealed class AngleTypeParser : TypeParser<Angle>
{
    public override bool TryParse(ParserContext parserContext, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var word = parserContext.GetWord(ParserContext.IsNumeric)?.ToLowerInvariant();
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
                error = new InvalidAngle(parserContext.GetWord()!);
                result = null;
                return false;
            }
        }

        if (word.EndsWith("deg"))
        {
            if (!float.TryParse(word[..^3], CultureInfo.InvariantCulture, out var f))
            {
                error = new InvalidAngle(word);
                result = null;
                return false;
            }

            result = Angle.FromDegrees(f);
            error = null;
            return true;
        }
        else
        {
            if (!float.TryParse(word, CultureInfo.InvariantCulture, out var f))
            {
                error = new InvalidAngle(word);
                result = null;
                return false;
            }

            result = new Angle(f);
            error = null;
            return true;
        }
    }

    public override ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ParserContext parserContext, string? argName)
    {
        return new ValueTask<(CompletionResult? result, IConError? error)>((CompletionResult.FromHint("angle (append deg for degrees, otherwise radians)"), null));
    }
}

public record InvalidAngle(string Value) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkup(
            $"The value {Value} is not a valid angle.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
