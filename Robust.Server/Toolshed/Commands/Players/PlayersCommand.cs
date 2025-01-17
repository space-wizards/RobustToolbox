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
public sealed class PlayerCommand : ToolshedCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

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

    [CommandImplementation("imm")]
    public ICommonSession Immediate(IInvocationContext ctx, string username)
    {
        _playerManager.TryGetSessionByUsername(username, out var session);

        if (session is null)
        {
            if (Guid.TryParse(username, out var guid))
            {
                _playerManager.TryGetSessionById(new NetUserId(guid), out session);
            }
        }

        if (session is null)
        {
            ctx.ReportError(new NoSuchPlayerError(username));
        }

        return session!;
    }

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
    public EntityUid GetPlayerEntity(IInvocationContext ctx, string username)
    {
        return GetPlayerEntity(Immediate(ctx, username));
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
