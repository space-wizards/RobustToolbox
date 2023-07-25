using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Maths;
using Robust.Shared.RTShell.Errors;
using Robust.Shared.Utility;

namespace Robust.Shared.RTShell.Commands.Info;

[ConsoleCommand]
internal sealed class SearchCommand : ConsoleCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<FormattedMessage> Search<T>([PipedArgument] IEnumerable<T> input, [CommandArgument] string term)
    {
        var list = input.Select(x => RtShell.PrettyPrintType(x!)).ToList();
        return list.Where(x => x.Contains(term, StringComparison.InvariantCultureIgnoreCase)).Select(x =>
        {
            var startIdx = x.IndexOf(term, StringComparison.InvariantCultureIgnoreCase);
            return ConHelpers.HighlightSpan(x, (startIdx, startIdx + term.Length), Color.Aqua);
        });
    }
}

