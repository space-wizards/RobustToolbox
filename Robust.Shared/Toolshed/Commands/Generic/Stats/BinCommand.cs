using System;
using System.Collections.Generic;

namespace Robust.Shared.Toolshed.Commands.Generic.Stats;

[ToolshedCommand]
public sealed class BinCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IDictionary<T, int> Bin<T>([PipedArgument] IEnumerable<T> input)
        where T: IComparable<T>
    {
        var dict = new Dictionary<T, int>();
        foreach (var v in input)
        {
            dict.TryAdd(v, 0);
            dict[v]++;
        }

        return dict;
    }
}
