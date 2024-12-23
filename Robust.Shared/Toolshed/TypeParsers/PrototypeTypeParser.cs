using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;

public sealed class ProtoIdTypeParser<T> : TypeParser<ProtoId<T>>
    where T : class, IPrototype
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private CompletionResult? _completions;

    public override bool TryParse(ParserContext ctx, out ProtoId<T> result)
    {
        result = default;
        string? proto;

        // Prototype ids can be specified without quotes, but for backwards compatibility, we also accept strings with
        // quotes, as previously it **had** to be a string
        if (ctx.PeekRune() == new Rune('"'))
        {
            if (!Toolshed.TryParse(ctx, out proto))
                return false;
        }
        else
        {
            proto = ctx.GetWord(ParserContext.IsToken);
        }

        if (proto is null || !_proto.HasIndex<T>(proto))
        {
            _proto.TryGetKindFrom<T>(out var kind);
            DebugTools.AssertNotNull(kind);

            ctx.Error = new NotAValidPrototype(proto ?? "[null]", kind!);
            result = default;
            return false;
        }

        result = new(proto);
        return true;
    }

    public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        if (_completions != null)
            return _completions;

        _proto.TryGetKindFrom<T>(out var kind);
        var hint = ToolshedCommand.GetArgHint(arg, typeof(ProtoId<T>));

        _completions = _proto.Count<T>() < 256
            ? CompletionResult.FromHintOptions( CompletionHelper.PrototypeIDs<T>(proto: _proto), hint)
            : CompletionResult.FromHint(hint);
        return _completions;
    }
}

public sealed class EntProtoIdTypeParser : TypeParser<EntProtoId>
{
    public override bool TryParse(ParserContext ctx, out EntProtoId result)
    {
        result = default;
        if (!Toolshed.TryParse(ctx, out ProtoId<EntityPrototype> proto))
            return false;

        result = new(proto.Id);
        return true;
    }

    public override CompletionResult? TryAutocomplete(ParserContext parserContext, CommandArgument? arg)
    {
        // TODO TOOLSHED Improve ProtoId completions
        // Completion options should be able to communicate to a client that it can populate the options by itself.
        // I.e., instead of dumping all entity prototypes on the client, tell it how to generate them locally.
        return CompletionResult.FromHint(GetArgHint(arg));
    }
}

public sealed class PrototypeInstanceTypeParser<T> : TypeParser<T>
    where T : class, IPrototype
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out T? result)
    {
        if (!Toolshed.TryParse(ctx, out string? proto))
            proto = ctx.GetWord(ParserContext.IsToken);

        if (proto != null && _proto.TryIndex(proto, out result))
            return true;

        _proto.TryGetKindFrom<T>(out var kind);
        DebugTools.AssertNotNull(kind);

        ctx.Error = new NotAValidPrototype(proto ?? "[null]", kind!);
        result = null;
        return false;
    }

    public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        return Toolshed.TryAutocomplete(ctx, typeof(ProtoId<T>), arg);
    }
}

[Obsolete]
internal sealed class PrototypeTypeParser<T> : TypeParser<Prototype<T>>
    where T : class, IPrototype
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override bool TryParse(ParserContext ctx, out Prototype<T> result)
    {
        if (!Toolshed.TryParse(ctx, out string? proto))
            proto = ctx.GetWord(ParserContext.IsToken);

        if (proto is null || !_prototype.TryIndex<T>(proto, out var resolved))
        {
            _prototype.TryGetKindFrom<T>(out var kind);
            DebugTools.AssertNotNull(kind);

            ctx.Error = new NotAValidPrototype(proto ?? "[null]", kind!);
            result = default;
            return false;
        }

        result = new Prototype<T>(resolved);
        return true;
    }

    public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        IEnumerable<CompletionOption> options;

        // todo: this should be an attribute.
        if (typeof(T) != typeof(EntityPrototype))
            options = CompletionHelper.PrototypeIDs<T>();
        else
            options = Array.Empty<CompletionOption>();

        _prototype.TryGetKindFrom<T>(out var kind);
        DebugTools.AssertNotNull(kind);

        return CompletionResult.FromHintOptions(options, $"<{kind} prototype>");
    }
}

[Obsolete("Use ProtoId<T> or EntProtoId, or the prototype directly")]
public readonly record struct Prototype<T>(T Value) : IAsType<string>
    where T : class, IPrototype
{
    public ProtoId<T> Id => Value.ID;

    public string AsType()
    {
        return Value.ID;
    }
}

public sealed class NotAValidPrototype(string proto, string kind) : ConError
{
    public override FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted($"{proto} is not a valid {kind} prototype");
    }
}
