using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Shared.Toolshed.Commands.Entities;

[ToolshedCommand]
internal sealed partial class DeleteCommand : ToolshedCommand
{
    [Dependency] private IEntityManager _entity = default!;

    [CommandImplementation]
    public void Delete([PipedArgument] IEnumerable<EntityUid> entities)
    {
        foreach (var ent in entities)
        {
            Del(ent);
        }
    }
}
