using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Shared.Map
{
    [TestFixture]
    public class GridRotation_Tests : RobustIntegrationTest
    {
        // Because integration tests are ten billion percent easier we'll just do all the rotation tests here.
        // These are mainly looking out for situations where the grid is rotated 90 / 180 degrees and we
        // need rotate poinst about the grid's origin which is a /very/ common source of bugs.

        [Test]
        public async Task Test()
        {
            var server = StartServer();

            await server.WaitIdleAsync();

            var entMan = server.ResolveDependency<IEntityManager>();
            var mapMan = server.ResolveDependency<IMapManager>();

            await server.WaitAssertion(() =>
            {
                var mapId = mapMan.CreateMap();
                var grid = mapMan.CreateGrid(mapId);
                var gridEnt = entMan.GetEntity(grid.GridEntityId);
                var coordinates = new EntityCoordinates(gridEnt.Uid, new Vector2(10, 0));

                // if no rotation and 0,0 position should just be the same coordinate.
                Assert.That(gridEnt.Transform.WorldRotation, Is.EqualTo(Angle.Zero));
                Assert.That(grid.WorldToLocal(coordinates.Position), Is.EqualTo(coordinates.Position));

                // Rotate 180 degrees should show -10, 0 for the position in map-terms and 10, 0 for the position in entity terms (i.e. no change).
                gridEnt.Transform.WorldRotation += new Angle(MathF.PI);
                Assert.That(gridEnt.Transform.WorldRotation, Is.EqualTo(new Angle(MathF.PI)));
                // Check the map coordinate rotates correctly
                Assert.That(grid.WorldToLocal(new Vector2(10, 0)).EqualsApprox(new Vector2(-10, 0)));
                Assert.That(grid.LocalToWorld(coordinates.Position).EqualsApprox(new Vector2(-10, 0)));

                // Now we'll do the same for 180 degrees.
                gridEnt.Transform.WorldRotation += MathF.PI / 2f;
                // If grid facing down then worldpos of 10, 0 gets rotated 90 degrees CCW and hence should be 0, 10
                Assert.That(grid.WorldToLocal(new Vector2(10, 0)).EqualsApprox(new Vector2(0, 10)));
                // If grid facing down then local 10,0 pos should just return 0, -10 given it's aligned with the rotation.
                Assert.That(grid.LocalToWorld(coordinates.Position).EqualsApprox(new Vector2(0, -10)));
            });
        }
    }
}
