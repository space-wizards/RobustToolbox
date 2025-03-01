using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
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
            var mapSystem = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<SharedMapSystem>();

            MapId mapId;
            Entity<MapGridComponent>? gridId1 = null;
            Entity<MapGridComponent>? gridId2 = null;
            PhysicsComponent? physics1 = null;
            PhysicsComponent? physics2 = null;
            EntityUid? gridEnt1;
            EntityUid? gridEnt2;

            await server.WaitPost(() =>
            {
                entManager.System<SharedMapSystem>().CreateMap(out mapId);
                gridId1 = mapManager.CreateGridEntity(mapId);
                gridId2 = mapManager.CreateGridEntity(mapId);
                gridEnt1 = gridId1.Value.Owner;
                gridEnt2 = gridId2.Value.Owner;
                physics1 = entManager.GetComponent<PhysicsComponent>(gridEnt1.Value);
                physics2 = entManager.GetComponent<PhysicsComponent>(gridEnt2.Value);
                // Can't collide static bodies and grids (at time of this writing) start as static
                // (given most other games would probably prefer them as static) hence we need to make them dynamic.
                physSystem.SetBodyType(gridEnt1.Value, BodyType.Dynamic, body: physics1);
                physSystem.SetBodyType(gridEnt2.Value, BodyType.Dynamic, body: physics2);
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

                    var bodyA = contact.BodyA;
                    var bodyB = contact.BodyB;

                    var other = physics1 == bodyA ? bodyB : bodyA;

                    Assert.That(other, Is.Not.EqualTo(physics2));
                }
            });

            await server.WaitAssertion(() =>
            {
                mapSystem.SetTile(gridId1!.Value, new Vector2i(0, 0), new Tile(1));
                mapSystem.SetTile(gridId2!.Value, new Vector2i(0, 0), new Tile(1));
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

                    var bodyA = contact.BodyA;
                    var bodyB = contact.BodyB;

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
