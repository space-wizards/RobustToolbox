using System.Collections.Generic;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Toolshed.Commands.Entities;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;

internal sealed class EntityTypeParser : TypeParser<EntityUid>
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    public static bool TryParseEntity(IEntityManager entMan, ParserContext ctx, out EntityUid result)
    {
        string? word;
        var start = ctx.Index;

        // e prefix implies we should parse the number as an EntityUid directly, not as a NetEntity
        // Note that this breaks auto completion results
        if (ctx.EatMatch('e'))
        {
            word = ctx.GetWord(ParserContext.IsToken);
            if (EntityUid.TryParse(word, out result))
                return true;

            ctx.Error = word is not null ? new InvalidEntity($"e{word}") : new OutOfInputError();
            ctx.Error.Contextualize(ctx.Input, (start, ctx.Index));
            return false;
        }

        // Optional 'n' prefix for differentiating whether an integer represents a NetEntity or EntityUid
        ctx.EatMatch('n');
        word = ctx.GetWord(ParserContext.IsToken);

        if (NetEntity.TryParse(word, out var ent))
        {
            result = entMan.GetEntity(ent);
            return true;
        }

        result = default;

        ctx.Error = word is not null ? new InvalidEntity(word) : new OutOfInputError();
        ctx.Error.Contextualize(ctx.Input, (start, ctx.Index));
        return false;
    }

    public override bool TryParse(ParserContext parser, out EntityUid result)
    {
        return TryParseEntity(_entMan, parser, out result);
    }

    public override CompletionResult TryAutocomplete(ParserContext ctx, CommandArgument? arg)
        => CompletionResult.FromHint(ToolshedCommand.GetArgHint(arg, typeof(NetEntity)));
}

internal sealed class NetEntityTypeParser : TypeParser<NetEntity>
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    public override bool TryParse(ParserContext ctx, out NetEntity result)
    {
        // This doesn't just directly call the EntityUid parser, as the client might be trying to parse a NetEntity to
        // send to the server, even though it doesn't actually know about / has not encountered.

        string? word;
        var start = ctx.Index;

        // e prefix implies we should parse the number as an EntityUid directly, not as a NetEntity
        // Note that this breaks auto completion results
        if (ctx.EatMatch('e'))
        {
            word = ctx.GetWord(ParserContext.IsToken);
            if (EntityUid.TryParse(word, out var euid))
            {
                result = _entMan.GetNetEntity(euid);
                return true;
            }

            result = default;
            ctx.Error = word is not null ? new InvalidEntity($"e{word}") : new OutOfInputError();
            ctx.Error.Contextualize(ctx.Input, (start, ctx.Index));
            return false;
        }

        // Optional 'n' prefix for differentiating whether an integer represents a NetEntity or EntityUid
        ctx.EatMatch('n');
        word = ctx.GetWord(ParserContext.IsToken);

        if (NetEntity.TryParse(word, out result))
            return true;

        result = default;

        ctx.Error = word is not null ? new InvalidEntity(word) : new OutOfInputError();
        ctx.Error.Contextualize(ctx.Input, (start, ctx.Index));
        return false;
    }

    public override CompletionResult TryAutocomplete(ParserContext ctx, CommandArgument? arg)
        => CompletionResult.FromHint(ToolshedCommand.GetArgHint(arg, typeof(NetEntity)));
}

internal sealed class EntityTypeParser<T> : TypeParser<Entity<T>>
    where T : IComponent
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    public override bool TryParse(ParserContext parser, out Entity<T> result)
    {
        result = default;
        if (!EntityTypeParser.TryParseEntity(_entMan, parser, out var uid))
            return false;

        if (!_entMan.TryGetComponent(uid, out T? comp))
            return false;

        result = new(uid, comp);
        return true;
    }

    public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        // Avoid commands with loose permissions accidentally leaking information about entities.
        // I.e., if some command had an Entity<MindComponent> argument, we don't want auto-completions for
        // that to allow people to get a list of all players/minds when they shouldn't know that.
        if (!ctx.CheckInvokable<EntitiesCommand>())
            return null;

        var hint = ToolshedCommand.GetArgHint(arg, typeof(NetEntity));

        // Avoid dumping too many entities
        if (_entMan.Count<T>() > 128)
            return CompletionResult.FromHint(hint);

        var query = _entMan.AllEntityQueryEnumerator<T, MetaDataComponent>();
        var list = new List<CompletionOption>();
        while (query.MoveNext(out _, out var metadata))
        {
            list.Add(new CompletionOption(metadata.NetEntity.ToString(), metadata.EntityName));
        }

        return CompletionResult.FromHintOptions(list, hint);
    }
}

internal sealed class EntityTypeParser<T1, T2> : TypeParser<Entity<T1, T2>>
    where T1 : IComponent
    where T2 : IComponent
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    public override bool TryParse(ParserContext parser, out Entity<T1, T2> result)
    {
        result = default;
        if (!EntityTypeParser.TryParseEntity(_entMan, parser, out var uid))
            return false;

        if (!_entMan.TryGetComponent(uid, out T1? comp1))
            return false;

        if (!_entMan.TryGetComponent(uid, out T2? comp2))
            return false;

        result = new(uid, comp1, comp2);
        return true;
    }

    public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        if (!ctx.CheckInvokable<EntitiesCommand>())
            return null;

        var hint = ToolshedCommand.GetArgHint(arg, typeof(NetEntity));
        if (_entMan.Count<T1>() > 128)
            return CompletionResult.FromHint(hint);

        var query = _entMan.AllEntityQueryEnumerator<T1, T2, MetaDataComponent>();
        var list = new List<CompletionOption>();
        while (query.MoveNext(out _, out _, out var metadata))
        {
            list.Add(new CompletionOption(metadata.NetEntity.ToString(), metadata.EntityName));
        }

        return CompletionResult.FromHintOptions(list, hint);
    }
}

internal sealed class EntityTypeParser<T1, T2, T3> : TypeParser<Entity<T1, T2, T3>>
    where T1 : IComponent
    where T2 : IComponent
    where T3 : IComponent
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    public override bool TryParse(ParserContext parser, out Entity<T1, T2, T3> result)
    {
        result = default;
        if (!EntityTypeParser.TryParseEntity(_entMan, parser, out var uid))
            return false;

        if (!_entMan.TryGetComponent(uid, out T1? comp1))
            return false;

        if (!_entMan.TryGetComponent(uid, out T2? comp2))
            return false;

        if (!_entMan.TryGetComponent(uid, out T3? comp3))
            return false;

        result = new(uid, comp1, comp2, comp3);
        return true;
    }

    public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        if (!ctx.CheckInvokable<EntitiesCommand>())
            return null;

        var hint = ToolshedCommand.GetArgHint(arg, typeof(NetEntity));
        if (_entMan.Count<T1>() > 128)
            return CompletionResult.FromHint(hint);

        var query = _entMan.AllEntityQueryEnumerator<T1, T2, T3, MetaDataComponent>();
        var list = new List<CompletionOption>();
        while (query.MoveNext(out _, out _, out _, out var metadata))
        {
            list.Add(new CompletionOption(metadata.NetEntity.ToString(), metadata.EntityName));
        }

        return CompletionResult.FromHintOptions(list, hint);
    }
}

public sealed class InvalidEntity(string value) : ConError
{
    public override FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted($"Couldn't parse {value} as an Entity.");
    }
}

public sealed class DeadEntity(EntityUid entity) : ConError
{
    public override FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted($"The entity {entity} does not exist.");
    }
}
