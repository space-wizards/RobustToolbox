using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Robust.Shared.Toolshed.Commands.Entities;

[ToolshedCommand]
internal sealed class PrototypedCommand : ToolshedCommand
{
    [CommandImplementation]
    public IEnumerable<EntityUid> Prototyped(
        [PipedArgument] IEnumerable<EntityUid> input,
        EntProtoId prototype,
        [CommandInverted] bool inverted)
    {
        // Note that this will never return deleted entities, even when inverted.

        if (input is not EntitiesCommand.AllEntityEnumerator)
        {
            return input.Where(x => TryComp(x, out MetaDataComponent? meta)
                                    && ((meta.EntityPrototype?.ID == prototype.Id) ^ inverted));
        }

        var ents = new List<EntityUid>();
        var query = EntityManager.AllEntityQueryEnumerator<MetaDataComponent>();
        while (query.MoveNext(out var uid, out var meta))
        {
            if ((meta.EntityPrototype?.ID == prototype.Id) ^ inverted)
                ents.Add(uid);
        }

        return ents;
    }

}
