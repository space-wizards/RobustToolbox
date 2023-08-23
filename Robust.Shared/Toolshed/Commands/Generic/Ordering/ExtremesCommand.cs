using System.Collections.Generic;
using System.Linq;

namespace Robust.Shared.Toolshed.Commands.Generic.Ordering;

[ToolshedCommand]
public sealed class ExtremesCommand : ToolshedCommand
{
    [TakesPipedTypeAsGeneric, CommandImplementation]
    public IEnumerable<T> Extremes<T>([PipedArgument] IEnumerable<T> input)
    {
        // Need something indexable for this to be valid.
        if (input is not IList<T> collection)
        {
            collection = input.ToArray();
        }

        var len = collection.Count;

        for (var i = 0; i < len / 2; i++)
        {
            yield return collection[i];
            yield return collection[^i];
        }

        if (collection.Count % 2 != 0)
        {
            yield return collection[collection.Count / 2];
        }
    }
}
