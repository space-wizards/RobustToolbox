using System;
using System.Numerics;
using NUnit.Framework;
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
            var mapManager = server.Resolve<IMapManager>();

            var mapId = mapManager.CreateMap();

            var ent1 = entManager.SpawnEntity(null, new MapCoordinates(Vector2.Zero, mapId));
            var ent2 = entManager.SpawnEntity(null, new MapCoordinates(new Vector2(100f, 0f), mapId));

            var xform1 = entManager.GetComponent<TransformComponent>(ent1);
            var xform2 = entManager.GetComponent<TransformComponent>(ent2);

            xform2.AttachParent(xform1);

            xform1.LocalRotation = MathF.PI;

            var (worldPos, worldRot, worldMatrix) = xform2.GetWorldPositionRotationMatrix();

            Assert.That(worldPos, Is.EqualTo(xform2.WorldPosition));
            Assert.That(worldRot, Is.EqualTo(xform2.WorldRotation));
            Assert.That(worldMatrix, Is.EqualTo(xform2.WorldMatrix));

            var (_, _, invWorldMatrix) = xform2.GetWorldPositionRotationInvMatrix();

            Assert.That(invWorldMatrix, Is.EqualTo(xform2.InvWorldMatrix));
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

            var mapId = mapManager.CreateMap();
            var grid = mapManager.CreateGridEntity(mapId);
            grid.Comp.SetTile(new Vector2i(0, 0), new Tile(1));
            var gridXform = entManager.GetComponent<TransformComponent>(grid);
            gridXform.LocalPosition = new Vector2(0f, 100f);

            var ent1 = entManager.SpawnEntity(null, new EntityCoordinates(grid, Vector2.One * grid.Comp.TileSize / 2));
            var ent2 = entManager.SpawnEntity(null, new EntityCoordinates(ent1, Vector2.Zero));

            var xform2 = entManager.GetComponent<TransformComponent>(ent2);
            Assert.That(xform2.WorldPosition, Is.EqualTo(new Vector2(0.5f, 100.5f)));

            xform2.AttachToGridOrMap();
            Assert.That(xform2.LocalPosition, Is.EqualTo(Vector2.One * grid.Comp.TileSize / 2));
        }
    }
}
