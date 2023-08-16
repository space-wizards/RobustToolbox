using System.Collections.Generic;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Toolshed.Commands.Entities.Components;

[ToolshedCommand]
internal sealed class AllCompsCommand : ToolshedCommand
{
    [CommandImplementation]
    public IEnumerable<IComponent> All([PipedArgument] EntityUid input)
        => EntityManager.GetComponents(input);
}
