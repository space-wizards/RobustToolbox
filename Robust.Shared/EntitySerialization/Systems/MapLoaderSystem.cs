using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Robust.Shared.EntitySerialization.Systems;

/// <summary>
/// This class provides methods for saving and loading maps and grids.
/// </summary>
/// <remarks>
/// The save & load methods are basically wrappers around <see cref="EntitySerializer"/> and
/// <see cref="EntityDeserializer"/>, which can be used for more control over serialization.
/// </remarks>
public sealed partial class MapLoaderSystem : EntitySystem
{
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly IDependencyCollection _dependency = default!;

    private Stopwatch _stopwatch = new();

    private EntityQuery<MapComponent> _mapQuery;
    private EntityQuery<MapGridComponent> _gridQuery;

    public override void Initialize()
    {
        base.Initialize();
        _gridQuery = GetEntityQuery<MapGridComponent>();
        _mapQuery = GetEntityQuery<MapComponent>();
        _gridQuery = GetEntityQuery<MapGridComponent>();
    }
}
