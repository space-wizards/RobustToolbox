using System;
using System.Collections.Generic;
using System.Numerics;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.Map;

[TestFixture]
public sealed class Query_Tests
{
    private static readonly TestCaseData[] Box2Data = new[]
    {
        new TestCaseData(
            Vector2.Zero,
            0f,
            Box2.UnitCentered.Translated(new Vector2(0f, 10f)),
            true
        ),

        new TestCaseData(
            Vector2.Zero,
            MathF.PI,
            Box2.UnitCentered.Translated(new Vector2(0f, 10f)),
            false
        ),

        new TestCaseData(
            Vector2.Zero,
            MathF.PI,
            Box2.UnitCentered.Translated(new Vector2(0f, -10f)),
            true
        ),

        new TestCaseData(
            Vector2.Zero,
            MathF.PI / 2f,
            Box2.UnitCentered.Translated(new Vector2(-10f, 0f)),
            true
        ),

        new TestCaseData(
            Vector2.Zero,
            MathF.PI / 4f,
            Box2.UnitCentered.Translated(new Vector2(-5f, 5f)),
            true
        ),
    };

    [Test, TestCaseSource(nameof(Box2Data))]
    public void TestBox2GridIntersection(Vector2 position, float radians, Box2 worldAABB, bool result)
    {
        var sim = RobustServerSimulation.NewSimulation().InitializeInstance();

        var entManager = sim.Resolve<IEntityManager>();
        var mapManager = sim.Resolve<IMapManager>();
        var mapSystem = entManager.System<SharedMapSystem>();
        var xformSystem = entManager.System<SharedTransformSystem>();

        var map = mapSystem.CreateMap();
        var grid = mapManager.CreateGridEntity(map);

        for (var i = 0; i < 10; i++)
        {
            mapSystem.SetTile(grid, new Vector2i(0, i), new Tile(1));
        }

        xformSystem.SetWorldRotation(grid.Owner, radians);

        var grids = new List<Entity<MapGridComponent>>();
        mapManager.FindGridsIntersecting(map, worldAABB, ref grids);

        Assert.That(grids.Count > 0, Is.EqualTo(result));
    }
}
