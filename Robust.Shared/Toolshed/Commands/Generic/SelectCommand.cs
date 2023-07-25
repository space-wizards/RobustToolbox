using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Toolshed.TypeParsers;

namespace Robust.Shared.Toolshed.Commands.Generic;

[RtShellCommand]
internal sealed class SelectCommand : ToolshedCommand
{
    [Dependency] private readonly IRobustRandom _random = default!;

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<TR> Select<TR>([PipedArgument] IEnumerable<TR> enumerable, [CommandArgument] Quantity quantity, [CommandInverted] bool inverted)
    {
        var arr = enumerable.ToArray();
        _random.Shuffle(arr);

        if (quantity is {Amount: { } amount})
        {
            var taken = (int) Math.Ceiling(amount);
            if (inverted)
                taken = Math.Max(0, arr.Length - taken);

            return arr.Take(taken);
        }
        else
        {
            var percent = inverted
                ? (int) Math.Floor(arr.Length * Math.Clamp(1 - (double) quantity.Percentage!, 0, 1))
                : (int) Math.Floor(arr.Length * Math.Clamp((double)  quantity.Percentage!, 0, 1));

            return arr.Take(percent);
        }
    }
}
