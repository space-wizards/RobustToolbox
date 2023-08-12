using System;
using System.Collections.Generic;

namespace Robust.Shared.Toolshed.Commands.Generic.Stats;

[ToolshedCommand]
public sealed class ClassifyCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IReadOnlyDictionary<T, int> Classify<T>([PipedArgument] IEnumerable<T> input)
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
