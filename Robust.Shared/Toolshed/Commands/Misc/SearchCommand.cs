using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.Commands.Misc;

[ToolshedCommand]
public sealed class SearchCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<FormattedMessage> Search<T>([PipedArgument] IEnumerable<T> input, string term)
    {
        var list = input.Select(x => Toolshed.PrettyPrintType(x, out _)).ToList();
        return list.Where(x => x.Contains(term, StringComparison.InvariantCultureIgnoreCase)).Select(x =>
        {
            var startIdx = x.IndexOf(term, StringComparison.InvariantCultureIgnoreCase);
            return ConHelpers.HighlightSpan(x, (startIdx, startIdx + term.Length), Color.Aqua);
        });
    }
}

