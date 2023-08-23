using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers.Math;

public sealed class ColorTypeParser : TypeParser<Color>
{
    public override bool TryParse(ParserContext parserContext, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var word = parserContext.GetWord(x => ParserContext.IsToken(x) || x == new Rune('#'))?.ToLowerInvariant();
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
                error = new InvalidColor(parserContext.GetWord()!);
                result = null;
                return false;
            }
        }

        if (!Color.TryParse(word, out var r))
        {
            error = new InvalidColor(word);
            result = null;
            return false;
        }

        result = r;
        error = null;
        return true;
    }

    public override ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ParserContext parserContext, string? argName)
    {
        return new ValueTask<(CompletionResult? result, IConError? error)>((
                CompletionResult.FromHintOptions(Color.GetAllDefaultColors().Select(x => x.Key), "RGB color or color name."),
                null
            ));
    }
}

public record InvalidColor(string Value) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkup(
            $"The value {Value} is not a valid RGB color or name of color.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
