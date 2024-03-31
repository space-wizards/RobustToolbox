using System;
using System.Collections.Generic;
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

internal sealed class PrototypeTypeParser<T> : TypeParser<Prototype<T>>
    where T : class, IPrototype
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override bool TryParse(ParserContext parserContext, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var proto = parserContext.GetWord(ParserContext.IsToken);

        if (proto is null || !_prototype.TryIndex<T>(proto, out var resolved))
        {
            _prototype.TryGetKindFrom<T>(out var kind);
            DebugTools.AssertNotNull(kind);

            error = new NotAValidPrototype(proto ?? "[null]", kind!);
            result = null;
            return false;
        }

        result = new Prototype<T>(resolved);
        error = null;
        return true;
    }

    public override ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ParserContext parserContext, string? argName)
    {
        IEnumerable<CompletionOption> options;

        // todo: this should be an attribute.
        if (typeof(T) != typeof(EntityPrototype))
            options = CompletionHelper.PrototypeIDs<T>();
        else
            options = Array.Empty<CompletionOption>();

        _prototype.TryGetKindFrom<T>(out var kind);
        DebugTools.AssertNotNull(kind);

        return ValueTask.FromResult<(CompletionResult? result, IConError? error)>((CompletionResult.FromHintOptions(options, $"<{kind} prototype>"), null));
    }
}

public readonly record struct Prototype<T>(T Value) : IAsType<string>
    where T : class, IPrototype
{
    public ProtoId<T> Id => Value.ID;

    public string AsType()
    {
        return Value.ID;
    }
}

public record NotAValidPrototype(string Proto, string Kind) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkup($"{Proto} is not a valid {Kind} prototype");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
