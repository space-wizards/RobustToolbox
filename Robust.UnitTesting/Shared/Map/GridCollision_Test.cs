using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Robust.UnitTesting.Shared.Map
{
    public sealed class GridCollision_Test : RobustIntegrationTest
    {
        [Test]
        public async Task TestGridsCollide()
        {
            var server = StartServer();

            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var entManager = server.ResolveDependency<IEntityManager>();
            var physSystem = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<SharedPhysicsSystem>();

            MapId mapId;
            MapGridComponent? gridId1 = null;
            MapGridComponent? gridId2 = null;
            PhysicsComponent? physics1 = null;
            PhysicsComponent? physics2 = null;
            EntityUid? gridEnt1;
            EntityUid? gridEnt2;

            await server.WaitPost(() => {
                mapId = mapManager.CreateMap();
                gridId1 = mapManager.CreateGrid(mapId);
                gridId2 = mapManager.CreateGrid(mapId);
                gridEnt1 = gridId1.GridEntityId;
                gridEnt2 = gridId2.GridEntityId;
                physics1 = entManager.GetComponent<PhysicsComponent>(gridEnt1.Value);
                physics2 = entManager.GetComponent<PhysicsComponent>(gridEnt2.Value);
                // Can't collide static bodies and grids (at time of this writing) start as static
                // (given most other games would probably prefer them as static) hence we need to make them dynamic.
                physSystem.SetBodyType(physics1, BodyType.Dynamic);
                physSystem.SetBodyType(physics2, BodyType.Dynamic);
            });

            await server.WaitRunTicks(1);

            // No tiles set hence should be no collision
            await server.WaitAssertion(() =>
            {
                var node = physics1?.Contacts.First;

                while (node != null)
                {
                    var contact = node.Value;
                    node = node.Next;

                    var bodyA = contact.FixtureA!.Body;
                    var bodyB = contact.FixtureB!.Body;

                    var other = physics1 == bodyA ? bodyB : bodyA;

                    Assert.That(other, Is.Not.EqualTo(physics2));
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
                var node = physics1?.Contacts.First;

                while (node != null)
                {
                    var contact = node.Value;
                    node = node.Next;

                    if (!contact.IsTouching)
                        continue;

                    var bodyA = contact.FixtureA!.Body;
                    var bodyB = contact.FixtureB!.Body;

                    var other = physics1 == bodyA ? bodyB : bodyA;

                    if (other == physics2)
                    {
                        colliding = true;
                        break;
                    }
                }

                Assert.That(colliding);
            });
        }
    }
}
