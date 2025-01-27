using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Toolshed.Commands.Entities;

[ToolshedCommand]
internal sealed class NamedCommand : ToolshedCommand
{
    [CommandImplementation]
    public IEnumerable<EntityUid> Named([PipedArgument] IEnumerable<EntityUid> input, string regex, [CommandInverted] bool inverted)
    {
        var compiled = new Regex($"^{regex}$");
        return input.Where(x => compiled.IsMatch(EntName(x)) ^ inverted);
    }
}
