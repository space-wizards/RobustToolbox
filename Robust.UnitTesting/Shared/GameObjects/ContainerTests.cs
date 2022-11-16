using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Client.GameObjects;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Network;
using MapSystem = Robust.Server.GameObjects.MapSystem;

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
                 var containerSys = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedContainerSystem>();

                 mapId = mapMan.CreateMap();
                 mapPos = new MapCoordinates((0, 0), mapId);

                 entityUid = entMan.SpawnEntity(null, mapPos);
                 entMan.GetComponent<MetaDataComponent>(entityUid).EntityName = "Container";
                 containerSys.EnsureContainer<Container>(entityUid, "dummy");

                 // Setup PVS
                 entMan.AddComponent<Robust.Server.GameObjects.EyeComponent>(entityUid);
                 var player = playerMan.ServerSessions.First();
                 player.AttachToEntity(entityUid);
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
                 var containerSys = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedContainerSystem>();

                 itemUid = entMan.SpawnEntity(null, mapPos);
                 entMan.GetComponent<MetaDataComponent>(itemUid).EntityName = "Item";
                 var container = containerSys.EnsureContainer<Container>(entityUid, "dummy");
                 Assert.That(container.Insert(itemUid));

                 // Move item out of PVS so that it doesn't get sent to the client
                 entMan.GetComponent<TransformComponent>(itemUid).LocalPosition = (100000, 0);
             });

             // Needs minimum 4 to sync to client because buffer size is 3
             await server.WaitRunTicks(10);
             await client.WaitRunTicks(40);

             await client.WaitAssertion(() =>
             {
                 var entMan = IoCManager.Resolve<IEntityManager>();
                 if (!entMan.TryGetComponent<ContainerManagerComponent>(entityUid, out var containerManagerComp))
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
                 if (!entMan.TryGetComponent<ContainerManagerComponent>(entityUid, out var containerManagerComp))
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
                 var containerSys = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedContainerSystem>();

                 mapId = mapMan.CreateMap();
                 mapPos = new MapCoordinates((0, 0), mapId);

                 entityUid = entMan.SpawnEntity(null, mapPos);
                 entMan.GetComponent<MetaDataComponent>(entityUid).EntityName = "Container";
                 containerSys.EnsureContainer<Container>(entityUid, "dummy");

                 // Setup PVS
                 entMan.AddComponent<Robust.Server.GameObjects.EyeComponent>(entityUid);
                 var player = playerMan.ServerSessions.First();
                 player.AttachToEntity(entityUid);
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
                 var containerSys = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedContainerSystem>();

                 itemUid = entMan.SpawnEntity(null, mapPos);
                 entMan.GetComponent<MetaDataComponent>(itemUid).EntityName = "Item";
                 var container = containerSys.EnsureContainer<Container>(entityUid, "dummy");
                 container.Insert(itemUid);

                 // Move item out of PVS so that it doesn't get sent to the client
                 entMan.GetComponent<TransformComponent>(itemUid).LocalPosition = (100000, 0);
             });

             // Needs minimum 4 to sync to client because buffer size is 3
             await server.WaitRunTicks(1);
             await client.WaitRunTicks(4);

             await client.WaitAssertion(() =>
             {
                 var entMan = IoCManager.Resolve<IEntityManager>();
                 if (!entMan.TryGetComponent<ContainerManagerComponent>(entityUid, out var containerManagerComp))
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
                 var containerSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedContainerSystem>();

                 // If possible it'd be best to only have the DeleteEntity, but right now
                 // the entity deleted event is not played on the client if the entity does not exist on the client.
                 if (entMan.EntityExists(itemUid)
                     // && itemUid.TryGetContainer(out var container))
                     && containerSystem.TryGetContainingContainer(itemUid, out var container))
                     container.ForceRemove(itemUid);
                 entMan.DeleteEntity(itemUid);
             });

             await server.WaitRunTicks(1);
             await client.WaitRunTicks(4);

             await client.WaitAssertion(() =>
             {
                 var entMan = IoCManager.Resolve<IEntityManager>();
                 if (!entMan.TryGetComponent<ContainerManagerComponent>(entityUid, out var containerManagerComp))
                 {
                     Assert.Fail();
                     return;
                 }

                 var container = containerManagerComp.GetContainer("dummy");
                 Assert.That(container.ContainedEntities.Count, Is.EqualTo(0));
                 Assert.That(container.ExpectedEntities.Count, Is.EqualTo(0));

                 var containerSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ContainerSystem>();
                 Assert.That(!containerSystem.ExpectedEntities.ContainsKey(itemUid));
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

            await server.WaitAssertion(() =>
            {
                var entMan = IoCManager.Resolve<IEntityManager>();
                var containerSys = entMan.EntitySysManager.GetEntitySystem<Robust.Server.Containers.ContainerSystem>();

                // build the map
                var mapIdOne = new MapId(1);
                var mapManager = IoCManager.Resolve<IMapManager>();

                mapManager.CreateMap(mapIdOne);
                Assert.That(mapManager.IsMapInitialized(mapIdOne), Is.True);

                var containerEnt = entMan.SpawnEntity(null, new MapCoordinates(1, 1, mapIdOne));
                entMan.GetComponent<MetaDataComponent>(containerEnt).EntityName = "ContainerEnt";

                var containeeEnt = entMan.SpawnEntity(null, new MapCoordinates(2, 2, mapIdOne));
                entMan.GetComponent<MetaDataComponent>(containeeEnt).EntityName = "ContaineeEnt";

                var container = containerSys.MakeContainer<Container>(containerEnt, "testContainer");
                container.OccludesLight = true;
                container.ShowContents = true;
                container.Insert(containeeEnt);

                // save the map
                var mapLoader = entMan.EntitySysManager.GetEntitySystem<MapLoaderSystem>();

                mapLoader.SaveMap(mapIdOne, "container_test.yml");
                mapManager.DeleteMap(mapIdOne);
            });

            // A few moments later...
            await server.WaitRunTicks(10);

            await server.WaitAssertion(() =>
            {
                var mapIdTwo = new MapId(2);
                var mapManager = IoCManager.Resolve<IMapManager>();
                var mapLoader = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<MapLoaderSystem>();

                // load the map
                mapLoader.Load(mapIdTwo, "container_test.yml");
                Assert.That(mapManager.IsMapInitialized(mapIdTwo), Is.True); // Map Initialize-ness is saved in the map file.
            });

            await server.WaitRunTicks(1);

            await server.WaitAssertion(() =>
            {
                var entMan = IoCManager.Resolve<IEntityManager>();

                // verify container
                var containerQuery = entMan.EntityQuery<ContainerManagerComponent>();
                var containerComp = containerQuery.First();
                var containerEnt = containerComp.Owner;

                Assert.That(entMan.GetComponent<MetaDataComponent>(containerEnt).EntityName, Is.EqualTo("ContainerEnt"));

                Assert.That(containerComp.Containers.ContainsKey("testContainer"));

                var iContainer = containerComp.GetContainer("testContainer");
                Assert.That(iContainer.ContainedEntities.Count, Is.EqualTo(1));

                var containeeEnt = iContainer.ContainedEntities[0];
                Assert.That(entMan.GetComponent<MetaDataComponent>(containeeEnt).EntityName, Is.EqualTo("ContaineeEnt"));
            });
        }
    }
}
