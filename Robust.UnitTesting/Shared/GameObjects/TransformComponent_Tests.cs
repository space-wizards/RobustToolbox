using System;
using System.Numerics;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.GameObjects
{
    [TestFixture]
    public sealed class TransformComponent_Tests
    {
        /// <summary>
        /// Verify that WorldPosition and WorldRotation return the same result as the faster helper method.
        /// </summary>
        [Test]
        public void TestGetWorldMatches()
        {
            var server = RobustServerSimulation.NewSimulation().InitializeInstance();

            var entManager = server.Resolve<IEntityManager>();
            entManager.System<SharedMapSystem>().CreateMap(out var mapId);
            var xform = entManager.System<TransformSystem>();

            var ent1 = entManager.SpawnEntity(null, new MapCoordinates(Vector2.Zero, mapId));
            var ent2 = entManager.SpawnEntity(null, new MapCoordinates(new Vector2(100f, 0f), mapId));

            var xform1 = entManager.GetComponent<TransformComponent>(ent1);
            var xform2 = entManager.GetComponent<TransformComponent>(ent2);

            xform.SetParent(ent2, ent1);

            xform1.LocalRotation = MathF.PI;

            var (worldPos, worldRot, worldMatrix) = xform.GetWorldPositionRotationMatrix(xform2);

            Assert.That(worldPos, Is.EqualTo(xform.GetWorldPosition(xform2)));
            Assert.That(worldRot, Is.EqualTo(xform.GetWorldRotation(xform2)));
            Assert.That(worldMatrix, Is.EqualTo(xform.GetWorldMatrix(xform2)));

            var (_, _, invWorldMatrix) = xform.GetWorldPositionRotationInvMatrix(xform2);

            Assert.That(invWorldMatrix, Is.EqualTo(xform.GetInvWorldMatrix(xform2)));
        }

        /// <summary>
        /// Asserts that when AttachToGridOrMap is called the entity remains in the same position.
        /// </summary>
        [Test]
        public void AttachToGridOrMap()
        {
            var server = RobustServerSimulation.NewSimulation().InitializeInstance();

            var entManager = server.Resolve<IEntityManager>();
            var mapManager = server.Resolve<IMapManager>();
            var mapSystem = entManager.System<SharedMapSystem>();
            var xformSystem = entManager.System<TransformSystem>();

            mapSystem.CreateMap(out var mapId);
            var grid = mapManager.CreateGridEntity(mapId);
            mapSystem.SetTile(grid, new Vector2i(0, 0), new Tile(1));
            xformSystem.SetLocalPosition(grid, new Vector2(0f, 100f));

            var ent1 = entManager.SpawnEntity(null, new EntityCoordinates(grid, Vector2.One * grid.Comp.TileSize / 2));
            var ent2 = entManager.SpawnEntity(null, new EntityCoordinates(ent1, Vector2.Zero));

            var xform2 = entManager.GetComponent<TransformComponent>(ent2);
            Assert.That(xformSystem.GetWorldPosition(ent2), Is.EqualTo(new Vector2(0.5f, 100.5f)));

            xformSystem.AttachToGridOrMap(ent2);
            Assert.That(xform2.LocalPosition, Is.EqualTo(Vector2.One * grid.Comp.TileSize / 2));
        }
    }
}
