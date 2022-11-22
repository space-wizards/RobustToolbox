using System.Linq;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.Map;

[TestFixture]
public sealed class MapGridMap_Tests
{
    /// <summary>
    /// Asserts FindGrids only returns the map once.
    /// </summary>
    [Test]
    public void FindGrids()
    {
        var sim = RobustServerSimulation.NewSimulation().InitializeInstance();

        var entManager = sim.Resolve<IEntityManager>();
        var mapManager = sim.Resolve<IMapManager>();

        var mapId = mapManager.CreateMap();
        Assert.That(!mapManager.FindGridsIntersecting(mapId, Box2.UnitCentered).Any());

        entManager.AddComponent<MapGridComponent>(mapManager.GetMapEntityId(mapId));
        Assert.That(mapManager.FindGridsIntersecting(mapId, Box2.UnitCentered).Count() == 1);
    }
}
