using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;

internal sealed class ProtoIdTypeParser<T> : TypeParser<ProtoId<T>>
    where T : class, IPrototype
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override bool TryParse(ParserContext parserContext, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var proto = parserContext.GetWord(ParserContext.IsToken);

        if (proto is null || !_prototype.HasMapping<T>(proto))
        {
            _prototype.TryGetKindFrom<T>(out var kind);
            DebugTools.AssertNotNull(kind);

            error = new NotAValidProtoId(proto ?? "[null]", kind!);
            result = null;
            return false;
        }

        result = new ProtoId<T>(proto);
        error = null;
        return true;
    }

    public override ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ParserContext parserContext, string? argName)
    {
        var options = CompletionHelper.PrototypeIDs<T>();

        _prototype.TryGetKindFrom<T>(out var kind);
        DebugTools.AssertNotNull(kind);

        return ValueTask.FromResult<(CompletionResult? result, IConError? error)>((CompletionResult.FromHintOptions(options, $"<{kind} protoId>"), null));
    }
}

public record NotAValidProtoId(string Proto, string Kind) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkup($"{Proto} is not a valid {Kind} prototype");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
