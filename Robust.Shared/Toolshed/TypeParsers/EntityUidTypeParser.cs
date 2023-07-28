using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;

internal sealed class EntityUidTypeParser : TypeParser<EntityUid>
{
    [Dependency] private readonly IEntityManager _entity = default!;

    public override bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var start = parser.Index;
        var word = parser.GetWord();
        error = null;

        if (!EntityUid.TryParse(word, out var ent))
        {
            result = null;

            if (word is not null)
                error = new InvalidEntityUid(word);
            else
                error = new OutOfInputError();

            error.Contextualize(parser.Input, (start, parser.Index));
            return false;
        }

        result = ent;
        return true;
    }

    public override async ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ForwardParser parser,
        string? argName)
    {
        return (CompletionResult.FromHint("<entity id>"), null);
    }
}

public record struct InvalidEntityUid(string Value) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkup($"Couldn't parse {Value} as an entity ID. Entity IDs are numeric, optionally starting with a c to indicate client-sided-ness.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}

public record struct DeadEntity(EntityUid Entity) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkup($"The entity {Entity} does not exist.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
