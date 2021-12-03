using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.UnitTesting.Shared.Map
{
    public class GridCollision_Test : RobustIntegrationTest
    {
        [Test]
        public async Task TestGridsCollide()
        {
            var server = StartServer();

            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var entManager = server.ResolveDependency<IEntityManager>();

            MapId mapId;
            IMapGrid? gridId1 = null;
            IMapGrid? gridId2 = null;
            PhysicsComponent? physics1 = null;
            PhysicsComponent? physics2 = null;
            IEntity? gridEnt1;
            IEntity? gridEnt2;

            await server.WaitPost(() =>
            {
                mapId = mapManager.CreateMap();
                gridId1 = mapManager.CreateGrid(mapId);
                gridId2 = mapManager.CreateGrid(mapId);
                gridEnt1 = entManager.GetEntity(gridId1.GridEntityId);
                gridEnt2 = entManager.GetEntity(gridId2.GridEntityId);
                physics1 = IoCManager.Resolve<IEntityManager>().GetComponent<PhysicsComponent>(gridEnt1);
                physics2 = IoCManager.Resolve<IEntityManager>().GetComponent<PhysicsComponent>(gridEnt2);
                // Can't collide static bodies and grids (at time of this writing) start as static
                // (given most other games would probably prefer them as static) hence we need to make them dynamic.
                physics1.BodyType = BodyType.Dynamic;
                physics2.BodyType = BodyType.Dynamic;
            });

            await server.WaitRunTicks(1);

            // No tiles set hence should be no collision
            await server.WaitAssertion(() =>
            {
                var edge = physics1?.ContactEdges;

                while (edge != null)
                {
                    Assert.That(edge.Other, Is.Not.EqualTo(physics2));
                    edge = edge.Next;
                }
            });

            await server.WaitAssertion(() =>
            {
                gridId1?.SetTile(new Vector2i(0, 0), new Tile(1));
                gridId2?.SetTile(new Vector2i(0, 0), new Tile(1));
            });

            await server.WaitRunTicks(1);

            await server.WaitAssertion(() =>
            {
                var colliding = false;
                var edge = physics1?.ContactEdges;

                while (edge != null)
                {
                    if (edge.Other == physics2)
                    {
                        colliding = true;
                        break;
                    }

                    edge = edge.Next;
                }

                Assert.That(colliding);
            });
        }
    }
}
