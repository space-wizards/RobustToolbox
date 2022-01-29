using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Shared
{
    [TestFixture, TestOf(typeof(IEntityLookup))]
    public sealed class EntityLookupTest : RobustIntegrationTest
    {
        /// <summary>
        /// Is the entity correctly removed / added to EntityLookup when anchored
        /// </summary>
        [Test]
        public async Task TestAnchoring()
        {
            var server = StartServer();
            await server.WaitIdleAsync();

            var lookup = server.ResolveDependency<IEntityLookup>();
            var entManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();

            await server.WaitAssertion(() =>
            {
                var mapId = mapManager.CreateMap();
                var grid = mapManager.CreateGrid(mapId);

                var theMapSpotBeingUsed = new Box2(Vector2.Zero, Vector2.One);
                grid.SetTile(new Vector2i(), new Tile(1));

                lookup.Update();
                Assert.That(lookup.GetEntitiesIntersecting(mapId, theMapSpotBeingUsed).ToList().Count, Is.EqualTo(1));

                // Setup and check it actually worked
                var dummy = entManager.SpawnEntity(null, new MapCoordinates(Vector2.Zero, mapId));
                lookup.Update();
                Assert.That(lookup.GetEntitiesIntersecting(mapId, theMapSpotBeingUsed).ToList().Count, Is.EqualTo(2));

                var xform = entManager.GetComponent<TransformComponent>(dummy);

                // When anchoring should still only be 1 entity.
                xform.Anchored = true;
                Assert.That(xform.Anchored);
                lookup.Update();
                Assert.That(lookup.GetEntitiesIntersecting(mapId, theMapSpotBeingUsed).ToList().Count, Is.EqualTo(2));

                // Even when unanchored should still be there
                xform.Anchored = false;
                lookup.Update();
                Assert.That(lookup.GetEntitiesIntersecting(mapId, theMapSpotBeingUsed).ToList().Count, Is.EqualTo(2));
            });
        }
    }
}
