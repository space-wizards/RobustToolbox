using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;

/// <summary>
/// Parse a username to an <see cref="ICommonSession"/>
/// </summary>
internal sealed partial class SessionTypeParser : TypeParser<ICommonSession>
{
    [Dependency] private ISharedPlayerManager _player = default!;

    public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out ICommonSession? result)
    {
        var start = ctx.Index;
        var word = ctx.GetWord();
        result = null;

        if (word == null)
        {
            ctx.Error = new OutOfInputError();
            return false;
        }

        if (_player.TryGetSessionByUsername(word, out var session))
        {
            result = session;
            return true;
        }

        if (Guid.TryParse(word, out var guid))
        {
            if (_player.TryGetSessionById(new NetUserId(guid), out session))
            {
                result = session;
                return true;
            }

            ctx.Error = new InvalidGuid(Loc, guid);
            ctx.Error.Contextualize(ctx.Input, (start, ctx.Index));
            return false;
        }

        ctx.Error = new InvalidUsername(Loc, word);
        ctx.Error.Contextualize(ctx.Input, (start, ctx.Index));
        return false;
    }

    public override CompletionResult TryAutocomplete(ParserContext parserContext, CommandArgument? arg)
    {
        var nameOpts = CompletionHelper.SessionNames(true, _player).ToList();
        var guidOpts = _player.Sessions
            .Select(session => new CompletionOption(session.UserId.UserId.ToString(), session.Name))
            .ToList();
        var opts = nameOpts.Concat(guidOpts).ToList(); // Combined list, with Guids sorted after all the names.

        return CompletionResult.FromHintOptions(opts, GetArgHint(arg));
    }

    public record InvalidUsername(ILocalizationManager Loc, string Username) : IConError
    {
        public FormattedMessage DescribeInner()
        {
            return FormattedMessage.FromUnformatted(Loc.GetString("cmd-parse-failure-session", ("username", Username)));
        }

        public string? Expression { get; set; }
        public Vector2i? IssueSpan { get; set; }
        public StackTrace? Trace { get; set; }
    }

    public record InvalidGuid(ILocalizationManager Loc, Guid Guid) : IConError
    {
        public FormattedMessage DescribeInner()
        {
            return FormattedMessage.FromUnformatted(Loc.GetString("cmd-parse-failure-session-guid", ("guid", Guid)));
        }
        public string? Expression { get; set; }
        public Vector2i? IssueSpan { get; set; }
        public StackTrace? Trace { get; set; }
    }
}
