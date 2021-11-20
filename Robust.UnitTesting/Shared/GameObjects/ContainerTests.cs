using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Client.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Network;

namespace Robust.UnitTesting.Shared.GameObjects
{
    public class ContainerTests : RobustIntegrationTest
    {
        /// <summary>
        /// Tests container states with children that do not exist on the client
        /// and tests that said children are added to the container when they do arrive on the client.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task TestContainerNonexistantItems()
        {
            var server = StartServer();
            var client = StartClient();

            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            Assert.DoesNotThrow(() => client.SetConnectTarget(server));
            client.Post(() => IoCManager.Resolve<IClientNetManager>().ClientConnect(null!, 0, null!));

            for (int i = 0; i < 10; i++)
            {
                await server.WaitRunTicks(1);
                await client.WaitRunTicks(1);
            }

            // Setup
            var mapId = MapId.Nullspace;
            var mapPos = MapCoordinates.Nullspace;

            EntityUid entityUid = default!;
            EntityUid itemUid = default!;

            await server.WaitAssertion(() =>
            {
                var mapMan = IoCManager.Resolve<IMapManager>();
                var entMan = IoCManager.Resolve<IEntityManager>();
                var playerMan = IoCManager.Resolve<IPlayerManager>();

                mapId = mapMan.CreateMap();
                mapPos = new MapCoordinates((0, 0), mapId);

                var entity = entMan.SpawnEntity(null, mapPos);
                entity.Name = "Container";
                entityUid = entity.Uid;
                var container = entity.EnsureContainer<Container>("dummy");

                // Setup PVS
                entity.AddComponent<Robust.Server.GameObjects.EyeComponent>();
                var player = playerMan.GetAllPlayers().First();
                player.AttachToEntity(entity);
                player.JoinGame();
            });

            for (int i = 0; i < 10; i++)
            {
                await server.WaitRunTicks(1);
                await client.WaitRunTicks(1);
            }

            await server.WaitAssertion(() =>
            {
                var entMan = IoCManager.Resolve<IEntityManager>();

                var item = entMan.SpawnEntity(null, mapPos);
                item.Name = "Item";
                itemUid = item.Uid;
                Assert.That(entMan.TryGetEntity(entityUid, out var entity));
                var container = entity!.EnsureContainer<Container>("dummy");
                container.Insert(item);

                // Move item out of PVS so that it doesn't get sent to the client
                item.Transform.LocalPosition = (100000, 0);
            });

            // Needs minimum 4 to sync to client because buffer size is 3
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(4);

            await client.WaitAssertion(() =>
            {
                var entMan = IoCManager.Resolve<IEntityManager>();
                if (!entMan.TryGetEntity(entityUid, out var entity)
                    || !entity.TryGetComponent<ContainerManagerComponent>(out var containerManagerComp))
                {
                    Assert.Fail();
                    return;
                }

                var container = containerManagerComp.GetContainer("dummy");
                Assert.That(container.ContainedEntities.Count, Is.EqualTo(0));
                Assert.That(container.ExpectedEntities.Count, Is.EqualTo(1));


                var containerSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ContainerSystem>();
                Assert.That(containerSystem.ExpectedEntities.ContainsKey(itemUid));
                Assert.That(containerSystem.ExpectedEntities.Count, Is.EqualTo(1));
            });

            await server.WaitAssertion(() =>
            {
                var entMan = IoCManager.Resolve<IEntityManager>();

                // Move item into PVS so it gets sent to the client
                entMan.GetComponent<TransformComponent>(itemUid).LocalPosition = (0, 0);
            });

            await server.WaitRunTicks(1);
            await client.WaitRunTicks(4);

            await client.WaitAssertion(() =>
            {
                var entMan = IoCManager.Resolve<IEntityManager>();
                if (!entMan.TryGetEntity(entityUid, out var entity)
                    || !entity.TryGetComponent<ContainerManagerComponent>(out var containerManagerComp))
                {
                    Assert.Fail();
                    return;
                }

                var container = containerManagerComp.GetContainer("dummy");
                Assert.That(container.ContainedEntities.Count, Is.EqualTo(1));
                Assert.That(container.ExpectedEntities.Count, Is.EqualTo(0));

                var containerSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ContainerSystem>();
                Assert.That(!containerSystem.ExpectedEntities.ContainsKey(itemUid));
                Assert.That(containerSystem.ExpectedEntities.Count, Is.EqualTo(0));
            });
        }

        /// <summary>
        /// Tests container states with children that do not exist on the client
        /// and that if those children are deleted that they get properly removed from the expected entities list.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task TestContainerExpectedEntityDeleted()
        {
            var server = StartServer();
            var client = StartClient();

            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            Assert.DoesNotThrow(() => client.SetConnectTarget(server));
            client.Post(() => IoCManager.Resolve<IClientNetManager>().ClientConnect(null!, 0, null!));

            for (int i = 0; i < 10; i++)
            {
                await server.WaitRunTicks(1);
                await client.WaitRunTicks(1);
            }

            // Setup
            var mapId = MapId.Nullspace;
            var mapPos = MapCoordinates.Nullspace;

            EntityUid entityUid = default!;
            EntityUid itemUid = default!;

            await server.WaitAssertion(() =>
            {
                var mapMan = IoCManager.Resolve<IMapManager>();
                var entMan = IoCManager.Resolve<IEntityManager>();
                var playerMan = IoCManager.Resolve<IPlayerManager>();

                mapId = mapMan.CreateMap();
                mapPos = new MapCoordinates((0, 0), mapId);

                var entity = entMan.SpawnEntity(null, mapPos);
                entity.Name = "Container";
                entityUid = entity.Uid;
                var container = entity.EnsureContainer<Container>("dummy");

                // Setup PVS
                entity.AddComponent<Robust.Server.GameObjects.EyeComponent>();
                var player = playerMan.GetAllPlayers().First();
                player.AttachToEntity(entity);
                player.JoinGame();
            });

            for (int i = 0; i < 10; i++)
            {
                await server.WaitRunTicks(1);
                await client.WaitRunTicks(1);
            }

            await server.WaitAssertion(() =>
            {
                var entMan = IoCManager.Resolve<IEntityManager>();

                var item = entMan.SpawnEntity(null, mapPos);
                item.Name = "Item";
                itemUid = item.Uid;
                Assert.That(entMan.TryGetEntity(entityUid, out var entity));
                var container = entity!.EnsureContainer<Container>("dummy");
                container.Insert(item);

                // Move item out of PVS so that it doesn't get sent to the client
                item.Transform.LocalPosition = (100000, 0);
            });

            // Needs minimum 4 to sync to client because buffer size is 3
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(4);

            await client.WaitAssertion(() =>
            {
                var entMan = IoCManager.Resolve<IEntityManager>();
                if (!entMan.TryGetEntity(entityUid, out var entity)
                    || !entity.TryGetComponent<ContainerManagerComponent>(out var containerManagerComp))
                {
                    Assert.Fail();
                    return;
                }

                var container = containerManagerComp.GetContainer("dummy");
                Assert.That(container.ContainedEntities.Count, Is.EqualTo(0));
                Assert.That(container.ExpectedEntities.Count, Is.EqualTo(1));

                var containerSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ContainerSystem>();
                Assert.That(containerSystem.ExpectedEntities.ContainsKey(itemUid));
                Assert.That(containerSystem.ExpectedEntities.Count, Is.EqualTo(1));
            });

            await server.WaitAssertion(() =>
            {
                var entMan = IoCManager.Resolve<IEntityManager>();

                // If possible it'd be best to only have the DeleteEntity, but right now
                // the entity deleted event is not played on the client if the entity does not exist on the client.
                if (entMan.TryGetEntity(itemUid, out var item)
                    && ContainerHelpers.TryGetContainer(item, out var container))
                    container.ForceRemove(item);
                entMan.DeleteEntity(itemUid);
            });

            await server.WaitRunTicks(1);
            await client.WaitRunTicks(4);

            await client.WaitAssertion(() =>
            {
                var entMan = IoCManager.Resolve<IEntityManager>();
                if (!entMan.TryGetEntity(entityUid, out var entity)
                    || !entity.TryGetComponent<ContainerManagerComponent>(out var containerManagerComp))
                {
                    Assert.Fail();
                    return;
                }

                var container = containerManagerComp.GetContainer("dummy");
                Assert.That(container.ContainedEntities.Count, Is.EqualTo(0));
                Assert.That(container.ExpectedEntities.Count, Is.EqualTo(0));

                /*
                var containerSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ContainerSystem>();
                Assert.That(!containerSystem.ExpectedEntities.ContainsKey(itemUid));
                Assert.That(containerSystem.ExpectedEntities.Count, Is.EqualTo(0));
                */
            });
        }

    }
}
