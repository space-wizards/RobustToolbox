using Robust.Shared.GameObjects;
using Robust.Shared.Toolshed.Errors;

namespace Robust.Shared.Toolshed.Commands.Players;

[ToolshedCommand]
internal sealed class SelfCommand : ToolshedCommand
{
    [CommandImplementation]
    public EntityUid Self(IInvocationContext ctx)
    {
        if (ctx.Session is null)
        {
            ctx.ReportError(new NotForServerConsoleError());
            return default!;
        }

        if (ctx.Session.AttachedEntity is { } ent)
            return ent;

        ctx.ReportError(new SessionHasNoEntityError(ctx.Session));
        return default!;
    }
}

