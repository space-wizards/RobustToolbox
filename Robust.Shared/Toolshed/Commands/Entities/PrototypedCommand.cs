using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed.TypeParsers;

namespace Robust.Shared.Toolshed.Commands.Entities;

[ToolshedCommand]
internal sealed class PrototypedCommand : ToolshedCommand
{
    [CommandImplementation]
    public IEnumerable<EntityUid> Prototyped(
            [PipedArgument] IEnumerable<EntityUid> input,
            [CommandArgument] Prototype<EntityPrototype> prototype,
            [CommandInverted] bool inverted
        )
        => input.Where(x => MetaData(x).EntityPrototype?.ID == prototype.Value.ID ^ inverted);

}
