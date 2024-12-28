using System.Collections.Generic;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Toolshed.Commands.Entities;

[ToolshedCommand]
internal sealed class DeleteCommand : ToolshedCommand
{
    [CommandImplementation]
    public void Delete([PipedArgument] IEnumerable<EntityUid> entities)
    {
        foreach (var ent in entities)
        {
            Del(ent);
        }
    }

    [CommandImplementation]
    public void Delete(EntityUid entity)
    {
        Del(entity);
    }
}
