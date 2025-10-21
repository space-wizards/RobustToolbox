using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Client.GameObjects;
using Robust.Client.Timing;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.GameObjects
{
    public sealed class ContainerTests : RobustIntegrationTest
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

             var cEntManager = client.ResolveDependency<IEntityManager>();
             var clientNetManager = client.ResolveDependency<IClientNetManager>();

             var sEntManager = server.ResolveDependency<IEntityManager>();
             var sPlayerManager = server.ResolveDependency<IPlayerManager>();

             Assert.DoesNotThrow(() => client.SetConnectTarget(server));
             client.Post(() =>
             {
                 clientNetManager.ClientConnect(null!, 0, null!);
             });

             for (int i = 0; i < 10; i++)
             {
                 await server.WaitRunTicks(1);
                 await client.WaitRunTicks(1);
             }

             // Setup
             var mapId = MapId.Nullspace;
             var mapPos = MapCoordinates.Nullspace;

             EntityUid entityUid = default!;

             var cContainerSys = cEntManager.System<ContainerSystem>();
             var sContainerSys = sEntManager.System<SharedContainerSystem>();
             var sMetadataSys = sEntManager.System<MetaDataSystem>();

             await server.WaitAssertion(() =>
             {
                 sEntManager.System<SharedMapSystem>().CreateMap(out mapId);
                 mapPos = new MapCoordinates(new Vector2(0, 0), mapId);

                 entityUid = sEntManager.SpawnEntity(null, mapPos);
                 sMetadataSys.SetEntityName(entityUid, "Container");
                 sContainerSys.EnsureContainer<Container>(entityUid, "dummy");

                 // Setup PVS
                 sEntManager.AddComponent<EyeComponent>(entityUid);
                 var player = sPlayerManager.Sessions.First();
                 server.PlayerMan.SetAttachedEntity(player, entityUid);
                 sPlayerManager.JoinGame(player);
             });

             for (int i = 0; i < 10; i++)
             {
                 await server.WaitRunTicks(1);
                 await client.WaitRunTicks(1);
             }

             EntityUid itemUid = default!;
             await server.WaitAssertion(() =>
             {
                 itemUid = sEntManager.SpawnEntity(null, mapPos);
                 sMetadataSys.SetEntityName(itemUid, "Item");
                 var container = sContainerSys.EnsureContainer<Container>(entityUid, "dummy");
                 Assert.That(sContainerSys.Insert(itemUid, container));

                 // Modify visibility layer so that the item does not get sent ot the player
                 sEntManager.System<SharedVisibilitySystem>().AddLayer(itemUid, 10 );
             });

             // Needs minimum 4 to sync to client because buffer size is 3
             await server.WaitRunTicks(4);
             await client.WaitRunTicks(10);

             EntityUid cEntityUid = default!;
             await client.WaitAssertion(() =>
             {
                 cEntityUid = client.EntMan.GetEntity(server.EntMan.GetNetEntity(entityUid));
                 if (!cEntManager.TryGetComponent<ContainerManagerComponent>(cEntityUid, out var containerManagerComp))
                 {
                     Assert.Fail();
                     return;
                 }

                 var container = cContainerSys.GetContainer(cEntityUid, "dummy", containerManagerComp);
                 Assert.That(container.ContainedEntities.Count, Is.EqualTo(0));
                 Assert.That(container.ExpectedEntities.Count, Is.EqualTo(1));

                 Assert.That(cContainerSys.ExpectedEntities.ContainsKey(sEntManager.GetNetEntity(itemUid)));
                 Assert.That(cContainerSys.ExpectedEntities.Count, Is.EqualTo(1));
             });

             await server.WaitAssertion(() =>
             {
                 // Modify visibility layer so it now gets sent to the client
                 sEntManager.System<SharedVisibilitySystem>().RemoveLayer(itemUid, 10 );
             });

             await server.WaitRunTicks(1);
             await client.WaitRunTicks(4);

             await client.WaitAssertion(() =>
             {
                 if (!cEntManager.TryGetComponent<ContainerManagerComponent>(cEntityUid, out var containerManagerComp))
                 {
                     Assert.Fail();
                     return;
                 }

                 var container = cContainerSys.GetContainer(cEntityUid, "dummy", containerManagerComp);
                 Assert.That(container.ContainedEntities.Count, Is.EqualTo(1));
                 Assert.That(container.ExpectedEntities.Count, Is.EqualTo(0));

                 Assert.That(!cContainerSys.ExpectedEntities.ContainsKey(sEntManager.GetNetEntity(itemUid)));
                 Assert.That(cContainerSys.ExpectedEntities, Is.Empty);
             });

             await client.WaitPost(() => clientNetManager.ClientDisconnect(""));
             await server.WaitRunTicks(5);
             await client.WaitRunTicks(5);
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

             var cEntManager = client.ResolveDependency<IEntityManager>();
             var clientTime = client.ResolveDependency<IClientGameTiming>();
             var clientNetManager = client.ResolveDependency<IClientNetManager>();

             var sMapManager = server.ResolveDependency<IMapManager>();
             var sEntManager = server.ResolveDependency<IEntityManager>();
             var sPlayerManager = server.ResolveDependency<IPlayerManager>();
             var serverTime = server.ResolveDependency<IGameTiming>();

             Assert.DoesNotThrow(() => client.SetConnectTarget(server));
             await client.WaitPost(() =>
             {
                 clientNetManager.ClientConnect(null!, 0, null!);
             });

             for (int i = 0; i < 10; i++)
             {
                 await server.WaitRunTicks(1);
                 await client.WaitRunTicks(1);
             }

             // Setup
             MapId mapId;
             var mapPos = MapCoordinates.Nullspace;

             EntityUid sEntityUid = default!;
             EntityUid sItemUid = default!;
             NetEntity netEnt = default;

             var cContainerSys = cEntManager.System<ContainerSystem>();
             var sContainerSys = sEntManager.System<SharedContainerSystem>();
             var sMetadataSys = sEntManager.System<MetaDataSystem>();

             await server.WaitAssertion(() =>
             {
                 sEntManager.System<SharedMapSystem>().CreateMap(out mapId);
                 mapPos = new MapCoordinates(new Vector2(0, 0), mapId);

                 sEntityUid = sEntManager.SpawnEntity(null, mapPos);
                 sMetadataSys.SetEntityName(sEntityUid, "Container");
                 sContainerSys.EnsureContainer<Container>(sEntityUid, "dummy");

                 // Setup PVS
                 sEntManager.AddComponent<EyeComponent>(sEntityUid);
                 var player = sPlayerManager.Sessions.First();
                 server.PlayerMan.SetAttachedEntity(player, sEntityUid);
                 sPlayerManager.JoinGame(player);
             });

             for (int i = 0; i < 10; i++)
             {
                 await server.WaitRunTicks(1);
                 await client.WaitRunTicks(1);
             }

             await server.WaitAssertion(() =>
             {
                 sItemUid = sEntManager.SpawnEntity(null, mapPos);
                 netEnt = sEntManager.GetNetEntity(sItemUid);
                 sMetadataSys.SetEntityName(sItemUid, "Item");
                 var container = sContainerSys.GetContainer(sEntityUid, "dummy");
                 sContainerSys.Insert(sItemUid, container);

                 // Modify visibility layer so that the item does not get sent ot the player
                 sEntManager.System<SharedVisibilitySystem>().AddLayer(sItemUid, 10 );
             });

            await server.WaitRunTicks(1);

            while (clientTime.LastRealTick < serverTime.CurTick - 1)
            {
                await client.WaitRunTicks(1);
            }

            var cUid = cEntManager.GetEntity(sEntManager.GetNetEntity(sEntityUid));

             await client.WaitAssertion(() =>
             {
                 if (!cEntManager.TryGetComponent<ContainerManagerComponent>(cUid, out var containerManagerComp))
                 {
                     Assert.Fail();
                     return;
                 }

                 var container = cContainerSys.GetContainer(cUid, "dummy", containerManagerComp);
                 Assert.That(container.ContainedEntities.Count, Is.EqualTo(0));
                 Assert.That(container.ExpectedEntities.Count, Is.EqualTo(1));

                 Assert.That(cContainerSys.ExpectedEntities.ContainsKey(netEnt));
                 Assert.That(cContainerSys.ExpectedEntities.Count, Is.EqualTo(1));
             });

             await server.WaitAssertion(() =>
             {
                 // If possible it'd be best to only have the DeleteEntity, but right now
                 // the entity deleted event is not played on the client if the entity does not exist on the client.
                 if (sEntManager.EntityExists(sItemUid)
                     // && itemUid.TryGetContainer(out var container))
                     && sContainerSys.TryGetContainingContainer(sItemUid, out var container))
                 {
                     sContainerSys.Remove(sItemUid, container, force: true);
                 }

                 sEntManager.DeleteEntity(sItemUid);
             });

             await server.WaitRunTicks(1);
             await client.WaitRunTicks(4);

             await client.WaitAssertion(() =>
             {
                 if (!cEntManager.TryGetComponent<ContainerManagerComponent>(cUid, out var containerManagerComp))
                 {
                     Assert.Fail();
                     return;
                 }

                 var container = cContainerSys.GetContainer(cUid, "dummy", containerManagerComp);
                 Assert.That(container.ContainedEntities.Count, Is.EqualTo(0));
                 Assert.That(container.ExpectedEntities.Count, Is.EqualTo(0));

                 Assert.That(!cContainerSys.ExpectedEntities.ContainsKey(netEnt));
                 Assert.That(cContainerSys.ExpectedEntities.Count, Is.EqualTo(0));
             });

             await client.WaitPost(() => clientNetManager.ClientDisconnect(""));
             await server.WaitRunTicks(5);
             await client.WaitRunTicks(5);
        }

        /// <summary>
        /// Sets up a new container, initializes map, saves the map, then loads it again on another map. The contained entity should still
        /// be inside the container.
        /// </summary>
        [Test]
        public async Task Container_DeserializeGrid_IsStillContained()
        {
            var server = StartServer();

            await Task.WhenAll(server.WaitIdleAsync());

            var sEntManager = server.ResolveDependency<IEntityManager>();
            var mapSys = sEntManager.System<SharedMapSystem>();
            var sContainerSys = sEntManager.System<SharedContainerSystem>();
            var sMetadataSys = sEntManager.System<MetaDataSystem>();
            var path = new ResPath("container_test.yml");

            await server.WaitAssertion(() =>
            {
                // build the map
                sEntManager.System<SharedMapSystem>().CreateMap(out var mapIdOne);
                Assert.That(mapSys.IsInitialized(mapIdOne), Is.True);

                var containerEnt = sEntManager.SpawnEntity(null, new MapCoordinates(1, 1, mapIdOne));
                sMetadataSys.SetEntityName(containerEnt, "ContainerEnt");

                var containeeEnt = sEntManager.SpawnEntity(null, new MapCoordinates(2, 2, mapIdOne));
                sMetadataSys.SetEntityName(containeeEnt, "ContaineeEnt");

                var container = sContainerSys.MakeContainer<Container>(containerEnt, "testContainer");
                container.OccludesLight = true;
                container.ShowContents = true;
                sContainerSys.Insert(containeeEnt, container);

                // save the map
                var mapLoader = sEntManager.EntitySysManager.GetEntitySystem<MapLoaderSystem>();

                Assert.That(mapLoader.TrySaveMap(mapIdOne, path));
                mapSys.DeleteMap(mapIdOne);
            });

            // A few moments later...
            await server.WaitRunTicks(10);

            await server.WaitAssertion(() =>
            {
                var mapLoader = sEntManager.System<MapLoaderSystem>();

                // load the map
                Assert.That(mapLoader.TryLoadMap(path, out var map, out _));
                Assert.That(mapSys.IsInitialized(map), Is.True); // Map Initialize-ness is saved in the map file.
            });

            await server.WaitRunTicks(1);

            await server.WaitAssertion(() =>
            {
                // verify container
                Entity<ContainerManagerComponent> container = default;
                var query = sEntManager.EntityQueryEnumerator<ContainerManagerComponent>();
                while (query.MoveNext(out var uid, out var containerComp))
                {
                    container = (uid, containerComp);
                }

                var containerEnt = container.Owner;
                Assert.That(container.Comp, Is.Not.Null);

                Assert.That(sEntManager.GetComponent<MetaDataComponent>(containerEnt).EntityName, Is.EqualTo("ContainerEnt"));

                Assert.That(container.Comp!.Containers.ContainsKey("testContainer"));

                var baseContainer = sContainerSys.GetContainer(containerEnt, "testContainer", container.Comp);
                Assert.That(baseContainer.ContainedEntities, Has.Count.EqualTo(1));

                var containeeEnt = baseContainer.ContainedEntities[0];
                Assert.That(sEntManager.GetComponent<MetaDataComponent>(containeeEnt).EntityName, Is.EqualTo("ContaineeEnt"));
            });
        }
    }
}
