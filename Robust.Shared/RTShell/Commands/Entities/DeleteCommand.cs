using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Shared.RTShell.Commands.Entities;

[ConsoleCommand]
public sealed class DeleteCommand : ConsoleCommand
{
    [Dependency] private readonly IEntityManager _entity = default!;

    [CommandImplementation]
    public void Delete([PipedArgument] IEnumerable<EntityUid> entities)
    {
        foreach (var ent in entities)
        {
            Del(ent);
        }
    }
}
