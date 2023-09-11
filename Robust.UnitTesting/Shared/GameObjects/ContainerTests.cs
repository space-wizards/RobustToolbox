using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Client.GameObjects;
using Robust.Client.Timing;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Timing;

// ReSharper disable AccessToStaticMemberViaDerivedType

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

             var sMapManager = server.ResolveDependency<IMapManager>();
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

             await server.WaitAssertion(() =>
             {
                 var containerSys = sEntManager.System<SharedContainerSystem>();

                 mapId = sMapManager.CreateMap();
                 mapPos = new MapCoordinates(new Vector2(0, 0), mapId);

                 entityUid = sEntManager.SpawnEntity(null, mapPos);
                 sEntManager.GetComponent<MetaDataComponent>(entityUid).EntityName = "Container";
                 containerSys.EnsureContainer<Container>(entityUid, "dummy");

                 // Setup PVS
                 sEntManager.AddComponent<EyeComponent>(entityUid);
                 var player = sPlayerManager.ServerSessions.First();
                 player.AttachToEntity(entityUid);
                 player.JoinGame();
             });

             for (int i = 0; i < 10; i++)
             {
                 await server.WaitRunTicks(1);
                 await client.WaitRunTicks(1);
             }

             EntityUid itemUid = default!;
             await server.WaitAssertion(() =>
             {
                 var containerSys = sEntManager.System<SharedContainerSystem>();

                 itemUid = sEntManager.SpawnEntity(null, mapPos);
                 sEntManager.GetComponent<MetaDataComponent>(itemUid).EntityName = "Item";
                 var container = containerSys.EnsureContainer<Container>(entityUid, "dummy");
                 Assert.That(container.Insert(itemUid));

                 // Move item out of PVS so that it doesn't get sent to the client
                 sEntManager.GetComponent<TransformComponent>(itemUid).LocalPosition = new Vector2(100000, 0);
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

                 var container = containerManagerComp.GetContainer("dummy");
                 Assert.That(container.ContainedEntities.Count, Is.EqualTo(0));
                 Assert.That(container.ExpectedEntities.Count, Is.EqualTo(1));

                 var containerSystem = cEntManager.System<ContainerSystem>();
                 Assert.That(containerSystem.ExpectedEntities.ContainsKey(sEntManager.GetNetEntity(itemUid)));
                 Assert.That(containerSystem.ExpectedEntities.Count, Is.EqualTo(1));
             });

             await server.WaitAssertion(() =>
             {
                 // Move item into PVS so it gets sent to the client
                 sEntManager.GetComponent<TransformComponent>(itemUid).LocalPosition = new Vector2(0, 0);
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

                 var container = containerManagerComp.GetContainer("dummy");
                 Assert.That(container.ContainedEntities.Count, Is.EqualTo(1));
                 Assert.That(container.ExpectedEntities.Count, Is.EqualTo(0));

                 var containerSystem = cEntManager.System<ContainerSystem>();
                 Assert.That(!containerSystem.ExpectedEntities.ContainsKey(sEntManager.GetNetEntity(itemUid)));
                 Assert.That(containerSystem.ExpectedEntities, Is.Empty);
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

             await server.WaitAssertion(() =>
             {
                 var containerSys = sEntManager.System<SharedContainerSystem>();

                 mapId = sMapManager.CreateMap();
                 mapPos = new MapCoordinates(new Vector2(0, 0), mapId);

                 sEntityUid = sEntManager.SpawnEntity(null, mapPos);
                 sEntManager.GetComponent<MetaDataComponent>(sEntityUid).EntityName = "Container";
                 containerSys.EnsureContainer<Container>(sEntityUid, "dummy");

                 // Setup PVS
                 sEntManager.AddComponent<EyeComponent>(sEntityUid);
                 var player = sPlayerManager.ServerSessions.First();
                 player.AttachToEntity(sEntityUid);
                 player.JoinGame();
             });

             for (int i = 0; i < 10; i++)
             {
                 await server.WaitRunTicks(1);
                 await client.WaitRunTicks(1);
             }

             await server.WaitAssertion(() =>
             {
                 var containerSys = sEntManager.System<SharedContainerSystem>();

                 sItemUid = sEntManager.SpawnEntity(null, mapPos);
                 netEnt = sEntManager.GetNetEntity(sItemUid);
                 sEntManager.GetComponent<MetaDataComponent>(sItemUid).EntityName = "Item";
                 var container = containerSys.GetContainer(sEntityUid, "dummy");
                 container.Insert(sItemUid);

                 // Move item out of PVS so that it doesn't get sent to the client
                 sEntManager.GetComponent<TransformComponent>(sItemUid).LocalPosition = new Vector2(100000, 0);
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

                 var container = containerManagerComp.GetContainer("dummy");
                 Assert.That(container.ContainedEntities.Count, Is.EqualTo(0));
                 Assert.That(container.ExpectedEntities.Count, Is.EqualTo(1));

                 var containerSystem = cEntManager.System<ContainerSystem>();
                 Assert.That(containerSystem.ExpectedEntities.ContainsKey(netEnt));
                 Assert.That(containerSystem.ExpectedEntities.Count, Is.EqualTo(1));
             });

             await server.WaitAssertion(() =>
             {
                 var containerSystem = sEntManager.System<SharedContainerSystem>();

                 // If possible it'd be best to only have the DeleteEntity, but right now
                 // the entity deleted event is not played on the client if the entity does not exist on the client.
                 if (sEntManager.EntityExists(sItemUid)
                     // && itemUid.TryGetContainer(out var container))
                     && containerSystem.TryGetContainingContainer(sItemUid, out var container))
                 {
                     container.ForceRemove(sItemUid);
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

                 var container = containerManagerComp.GetContainer("dummy");
                 Assert.That(container.ContainedEntities.Count, Is.EqualTo(0));
                 Assert.That(container.ExpectedEntities.Count, Is.EqualTo(0));

                 var containerSystem = cEntManager.System<ContainerSystem>();
                 Assert.That(!containerSystem.ExpectedEntities.ContainsKey(netEnt));
                 Assert.That(containerSystem.ExpectedEntities.Count, Is.EqualTo(0));
             });
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
            var mapManager = server.ResolveDependency<IMapManager>();

            await server.WaitAssertion(() =>
            {
                var containerSys = sEntManager.EntitySysManager.GetEntitySystem<Robust.Server.Containers.ContainerSystem>();

                // build the map
                var mapIdOne = mapManager.CreateMap();
                Assert.That(mapManager.IsMapInitialized(mapIdOne), Is.True);

                var containerEnt = sEntManager.SpawnEntity(null, new MapCoordinates(1, 1, mapIdOne));
                sEntManager.GetComponent<MetaDataComponent>(containerEnt).EntityName = "ContainerEnt";

                var containeeEnt = sEntManager.SpawnEntity(null, new MapCoordinates(2, 2, mapIdOne));
                sEntManager.GetComponent<MetaDataComponent>(containeeEnt).EntityName = "ContaineeEnt";

                var container = containerSys.MakeContainer<Container>(containerEnt, "testContainer");
                container.OccludesLight = true;
                container.ShowContents = true;
                container.Insert(containeeEnt);

                // save the map
                var mapLoader = sEntManager.EntitySysManager.GetEntitySystem<MapLoaderSystem>();

                mapLoader.SaveMap(mapIdOne, "container_test.yml");
                mapManager.DeleteMap(mapIdOne);
            });

            // A few moments later...
            await server.WaitRunTicks(10);

            await server.WaitAssertion(() =>
            {
                var mapLoader = sEntManager.System<MapLoaderSystem>();
                var mapIdTwo = mapManager.CreateMap();

                // load the map
                mapLoader.Load(mapIdTwo, "container_test.yml");
                Assert.That(mapManager.IsMapInitialized(mapIdTwo), Is.True); // Map Initialize-ness is saved in the map file.
            });

            await server.WaitRunTicks(1);

            await server.WaitAssertion(() =>
            {
                // verify container
                var containerQuery = sEntManager.EntityQuery<ContainerManagerComponent>();
                var containerComp = containerQuery.First();
                var containerEnt = containerComp.Owner;

                Assert.That(sEntManager.GetComponent<MetaDataComponent>(containerEnt).EntityName, Is.EqualTo("ContainerEnt"));

                Assert.That(containerComp.Containers.ContainsKey("testContainer"));

                var baseContainer = containerComp.GetContainer("testContainer");
                Assert.That(baseContainer.ContainedEntities, Has.Count.EqualTo(1));

                var containeeEnt = baseContainer.ContainedEntities[0];
                Assert.That(sEntManager.GetComponent<MetaDataComponent>(containeeEnt).EntityName, Is.EqualTo("ContaineeEnt"));
            });
        }
    }
}
