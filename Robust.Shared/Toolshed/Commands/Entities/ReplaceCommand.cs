using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed.TypeParsers;

namespace Robust.Shared.Toolshed.Commands.Entities;

[ToolshedCommand]
internal sealed class ReplaceCommand : ToolshedCommand
{
    [CommandImplementation]
    public IEnumerable<EntityUid> Replace(
            [PipedArgument] IEnumerable<EntityUid> input,
            [CommandArgument] Prototype<EntityPrototype> prototype
        )
    {
        foreach (var i in input)
        {
            var xform = Transform(i);
            var coords = xform.Coordinates;
            var rot = xform.LocalRotation;
            QDel(i); // yeet
            var res = Spawn(prototype.Value.ID, coords);
            Transform(res).LocalRotation = rot;
            yield return res;
        }
    }
}
