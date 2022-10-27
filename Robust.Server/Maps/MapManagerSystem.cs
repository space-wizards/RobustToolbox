using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Robust.Server.Maps;

public sealed partial class MapManagerSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
}
