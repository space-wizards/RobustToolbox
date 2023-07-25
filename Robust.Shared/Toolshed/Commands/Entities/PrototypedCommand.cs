using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Toolshed.Commands.Entities;

[ToolshedCommand]
internal sealed class PrototypedCommand : ToolshedCommand
{
    [CommandImplementation()]
    public IEnumerable<EntityUid> Prototyped([PipedArgument] IEnumerable<EntityUid> input,
        [CommandArgument] string prototype)
        => input.Where(x => MetaData(x).EntityPrototype?.ID == prototype);

}
