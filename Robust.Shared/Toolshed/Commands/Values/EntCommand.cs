using Robust.Shared.GameObjects;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Values;

[ToolshedCommand]
internal sealed class EntCommand : ToolshedCommand
{
    [CommandImplementation]
    public EntityUid Ent([CommandArgument] ValueRef<EntityUid> ent, [CommandInvocationContext] IInvocationContext ctx) => ent.Evaluate(ctx);
}

