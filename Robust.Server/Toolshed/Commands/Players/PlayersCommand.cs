using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Utility;

namespace Robust.Server.Toolshed.Commands.Players;

[ToolshedCommand]
public sealed partial class PlayerCommand : ToolshedCommand
{
    [Dependency] private IPlayerManager _playerManager = default!;

    [CommandImplementation("list")]
    public IEnumerable<ICommonSession> Players()
        => _playerManager.Sessions;

    [CommandImplementation("self")]
    public ICommonSession Self(IInvocationContext ctx)
    {
        if (ctx.Session is null)
        {
            ctx.ReportError(new NotForServerConsoleError());
        }

        return ctx.Session!;
    }

    /*
     * Pass in ICommonSession to get the... passed... ICommonSession...
     * Look that seems really stupid (because it is) but the Toolshed parser doesn't let you just type in a session
     * name at the start of a command, so immediate IS required in order to pipe to other commands. Luckily, Toolshed
     * DOES have a parser for ICommonSession that even comes with autocomplete. So as insane as it looks here to just
     * have a function that returns the exact thing you're passing to it, it's more efficient (and convenient)
     * than parsing a username string. Toolshed is fucking weird, man.
     */
    [CommandImplementation("imm")]
    public ICommonSession Immediate(IInvocationContext ctx, ICommonSession session) =>
        session;

    [CommandImplementation("entity")]
    public IEnumerable<EntityUid> GetPlayerEntity([PipedArgument] IEnumerable<ICommonSession> sessions)
    {
        return sessions.Select(x => x.AttachedEntity).Where(x => x is not null).Cast<EntityUid>();
    }

    [CommandImplementation("entity")]
    public EntityUid GetPlayerEntity([PipedArgument] ICommonSession sessions)
    {
        return sessions.AttachedEntity ?? default;
    }

    [CommandImplementation("entity")]
    public EntityUid GetPlayerEntity(IInvocationContext ctx, ICommonSession sessions)
    {
        return sessions.AttachedEntity ?? default;
    }
}

public record struct NoSuchPlayerError(string Username) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted($"No player with the username/GUID {Username} could be found.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
