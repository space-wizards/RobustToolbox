using System.Collections.Generic;
using System.Linq;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Toolshed;

namespace Robust.Server.Toolshed.Commands.Players;

[ToolshedCommand]
public sealed class ActorCommand : ToolshedCommand
{
    [CommandImplementation("controlled")]
    public IEnumerable<EntityUid> Controlled([PipedArgument] IEnumerable<EntityUid> input)
    {
        return input.Where(HasComp<ActorComponent>);
    }

    [CommandImplementation("session")]
    public IEnumerable<ICommonSession> Session([PipedArgument] IEnumerable<EntityUid> input)
    {
        return input.Where(HasComp<ActorComponent>).Select(x => Comp<ActorComponent>(x).PlayerSession);
    }
}
