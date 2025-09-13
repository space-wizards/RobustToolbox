using System.Linq;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.Commands.Misc;

[ToolshedCommand]
internal sealed class CatCommand : ToolshedCommand
{
    [Dependency] private readonly IResourceManager _res = default!;

    [CommandImplementation]
    public string Cat(params ResPath[] path)
    {
        return string.Concat(path.Select(x => _res.UserData.ReadAllText(x)));
    }
}
