using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;

internal sealed class EntityTypeParser : TypeParser<EntityUid>
{
    public override bool TryParse(ParserContext parser, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var start = parser.Index;
        var word = parser.GetWord(ParserContext.IsToken);
        error = null;

        if (!EntityUid.TryParse(word, out var ent))
        {
            result = null;

            if (word is not null)
                error = new InvalidEntity(word);
            else
                error = new OutOfInputError();

            error.Contextualize(parser.Input, (start, parser.Index));
            return false;
        }

        result = ent;
        return true;
    }

    public override async ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ParserContext parserContext,
        string? argName)
    {
        return (CompletionResult.FromHint("<NetEntity>"), null);
    }
}

public record InvalidEntity(string Value) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkup($"Couldn't parse {Value} as an Entity.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}

public record DeadEntity(EntityUid Entity) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkup($"The entity {Entity} does not exist.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
