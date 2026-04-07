using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Robust.Shared.Configuration;
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
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

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

    public override CompletionResult TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        var hint = ToolshedCommand.GetArgHint(arg, typeof(ProtoId<T>));
        var maxCount = _config.GetCVar(CVars.ToolshedPrototypesAutocompleteLimit);
        var options = CompletionHelper.PrototypeIdsLimited<T>(ctx.Input[ctx.Index..], proto: _proto, maxCount: maxCount);
        return CompletionResult.FromHintOptions(options, hint);
    }
}

public sealed class EntProtoIdTypeParser : TypeParser<EntProtoId>
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override bool TryParse(ParserContext ctx, out EntProtoId result)
    {
        result = default;
        if (!Toolshed.TryParse(ctx, out ProtoId<EntityPrototype> proto))
            return false;

        result = new(proto.Id);
        return true;
    }

    public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        return Toolshed.TryAutocomplete(ctx, typeof(ProtoId<EntityPrototype>), arg);
    }
}

public sealed class NotAValidPrototype(string proto, string kind) : ConError
{
    public override FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted($"{proto} is not a valid {kind} prototype");
    }
}
