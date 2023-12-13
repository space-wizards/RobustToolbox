using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Robust.Shared.Toolshed.Commands.Math;

[ToolshedCommand]
public sealed class AverageCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Average<T>([PipedArgument] IEnumerable<T> input)
        where T: INumberBase<T>
    {
        var a = input.ToArray();
        var sum = T.Zero;
        foreach (var value in a)
        {
            sum += value;
        }

        return sum / T.CreateChecked(a.Length);
    }
}
