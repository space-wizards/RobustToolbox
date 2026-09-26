using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Client.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.GameObjects
{
    internal sealed class ContainerTests : RobustIntegrationTest
    {
        private const string ContainerId = "dummy";
        private const string OtherContainerId = "other";
        private const int HiddenVisibilityLayer = 10;

        private async Task<(EntityUid Owner, MapCoordinates MapPosition)> SpawnContainerOwner(
            ServerIntegrationInstance server,
            IEntityManager entManager,
            IPlayerManager playerManager,
            SharedContainerSystem containerSystem,
            MetaDataSystem metadataSystem,
            string containerId = ContainerId)
        {
            var mapPos = MapCoordinates.Nullspace;
            EntityUid owner = default;

            await server.WaitAssertion(() =>
            {
                entManager.System<SharedMapSystem>().CreateMap(out var mapId);
                mapPos = new MapCoordinates(new Vector2(0, 0), mapId);

                owner = entManager.SpawnEntity(null, mapPos);
                metadataSystem.SetEntityName(owner, "Container");
                containerSystem.EnsureContainer<Container>(owner, containerId);

                entManager.AddComponent<EyeComponent>(owner);
                var player = playerManager.Sessions.First();
                server.PlayerMan.SetAttachedEntity(player, owner);
                playerManager.JoinGame(player);
            });

            return (owner, mapPos);
        }

        private static EntityUid GetClientEntity(IEntityManager entManager, NetEntity netEntity)
        {
            Assert.That(entManager.TryGetEntity(netEntity, out var entity), Is.True);
            return entity!.Value;
        }

        private static BaseContainer GetClientContainer(
            IEntityManager entManager,
            ContainerSystem containerSystem,
            NetEntity ownerNetEntity,
            string containerId = ContainerId)
        {
            return containerSystem.GetContainer(GetClientEntity(entManager, ownerNetEntity), containerId);
        }

        private static void AssertPendingContainerState(
            ClientEntityManager entManager,
            NetEntity missingEntity,
            EntityUid owner)
        {
            Assert.That(entManager.PendingNetEntityStates.TryGetValue(missingEntity, out var pending), Is.True);
            Assert.That(pending, Has.Some.Matches<(System.Type, EntityUid)>(entry =>
                entry.Item1 == typeof(ContainerManagerComponent) && entry.Item2 == owner));
        }

        /// <summary>
        /// Tests container states with children that do not exist on the client
        /// and tests that said children are added to the container when they do arrive on the client.
        /// </summary>
        [Test]
        public async Task ContainerStateMissingEntitySpawnsLater()
        {
            await using var pair = await StartConnectedPair();
            var server = pair.Server;
            var client = pair.Client;

            await RunTicksSync(server, client, 10);

            var cEntManager = client.ResolveDependency<IEntityManager>();
            var cClientEntManager = (ClientEntityManager) cEntManager;
            var cContainerSys = cEntManager.System<ContainerSystem>();

            var sEntManager = server.ResolveDependency<IEntityManager>();
            var sPlayerManager = server.ResolveDependency<IPlayerManager>();
            var sContainerSys = sEntManager.System<SharedContainerSystem>();
            var sMetadataSys = sEntManager.System<MetaDataSystem>();
            var sVisibilitySys = sEntManager.System<SharedVisibilitySystem>();

            var (owner, mapPos) = await SpawnContainerOwner(server, sEntManager, sPlayerManager, sContainerSys, sMetadataSys);
            var ownerNet = sEntManager.GetNetEntity(owner);

            await RunTicksSync(server, client, 10);
            var cOwner = GetClientEntity(cEntManager, ownerNet);

            EntityUid item = default;
            NetEntity itemNet = default;
            await server.WaitAssertion(() =>
            {
                item = sEntManager.SpawnEntity(null, mapPos);
                itemNet = sEntManager.GetNetEntity(item);
                sMetadataSys.SetEntityName(item, "Item");
                Assert.That(sContainerSys.Insert(item, sContainerSys.GetContainer(owner, ContainerId)));
                sVisibilitySys.AddLayer(item, HiddenVisibilityLayer);
            });

            await RunTicksSync(server, client, 10);

            await client.WaitAssertion(() =>
            {
                Assert.That(cEntManager.TryGetEntity(itemNet, out _), Is.False);

                var container = GetClientContainer(cEntManager, cContainerSys, ownerNet);
                Assert.That(container.ContainedEntities, Is.Empty);
                Assert.That(container.PvsDetachedEntities, Is.Empty);
                Assert.That(cContainerSys.PvsDetachedEntities, Is.Empty);
                AssertPendingContainerState(cClientEntManager, itemNet, cOwner);
            });

            await server.WaitAssertion(() =>
            {
                sVisibilitySys.RemoveLayer(item, HiddenVisibilityLayer);
            });

            await RunTicksSync(server, client, 10);

            await client.WaitAssertion(() =>
            {
                var cItem = GetClientEntity(cEntManager, itemNet);
                var container = GetClientContainer(cEntManager, cContainerSys, ownerNet);
                Assert.That(container.ContainedEntities, Has.Member(cItem));
                Assert.That(container.PvsDetachedEntities, Is.Empty);
                Assert.That(cContainerSys.PvsDetachedEntities, Is.Empty);
                Assert.That(cClientEntManager.PendingNetEntityStates.ContainsKey(itemNet), Is.False);
            });
        }

        /// <summary>
        /// Tests container states with children that do not exist on the client
        /// and that if those children move before being spawned, only the latest state is applied.
        /// </summary>
        [Test]
        public async Task ContainerStateMissingEntityMovesBeforeSpawn()
        {
            await using var pair = await StartConnectedPair();
            var server = pair.Server;
            var client = pair.Client;

            await RunTicksSync(server, client, 10);

            var cEntManager = client.ResolveDependency<IEntityManager>();
            var cClientEntManager = (ClientEntityManager) cEntManager;
            var cContainerSys = cEntManager.System<ContainerSystem>();

            var sEntManager = server.ResolveDependency<IEntityManager>();
            var sPlayerManager = server.ResolveDependency<IPlayerManager>();
            var sContainerSys = sEntManager.System<SharedContainerSystem>();
            var sMetadataSys = sEntManager.System<MetaDataSystem>();
            var sVisibilitySys = sEntManager.System<SharedVisibilitySystem>();

            var (owner, mapPos) = await SpawnContainerOwner(server, sEntManager, sPlayerManager, sContainerSys, sMetadataSys);
            var ownerNet = sEntManager.GetNetEntity(owner);

            await server.WaitAssertion(() =>
            {
                sContainerSys.EnsureContainer<Container>(owner, OtherContainerId);
            });

            await RunTicksSync(server, client, 10);
            var cOwner = GetClientEntity(cEntManager, ownerNet);

            EntityUid item = default;
            NetEntity itemNet = default;
            await server.WaitAssertion(() =>
            {
                item = sEntManager.SpawnEntity(null, mapPos);
                itemNet = sEntManager.GetNetEntity(item);
                sMetadataSys.SetEntityName(item, "Item");
                Assert.That(sContainerSys.Insert(item, sContainerSys.GetContainer(owner, ContainerId)));
                sVisibilitySys.AddLayer(item, HiddenVisibilityLayer);
            });

            await RunTicksSync(server, client, 10);

            await client.WaitAssertion(() =>
            {
                Assert.That(cEntManager.TryGetEntity(itemNet, out _), Is.False);
                Assert.That(GetClientContainer(cEntManager, cContainerSys, ownerNet).ContainedEntities, Is.Empty);
                Assert.That(GetClientContainer(cEntManager, cContainerSys, ownerNet, OtherContainerId).ContainedEntities, Is.Empty);
                Assert.That(cContainerSys.PvsDetachedEntities, Is.Empty);
                AssertPendingContainerState(cClientEntManager, itemNet, cOwner);
            });

            await server.WaitAssertion(() =>
            {
                sContainerSys.Remove(item, sContainerSys.GetContainer(owner, ContainerId), force: true);
                Assert.That(sContainerSys.Insert(item, sContainerSys.GetContainer(owner, OtherContainerId), force: true));
            });

            await RunTicksSync(server, client, 10);

            await client.WaitAssertion(() =>
            {
                Assert.That(cEntManager.TryGetEntity(itemNet, out _), Is.False);
                Assert.That(GetClientContainer(cEntManager, cContainerSys, ownerNet).ContainedEntities, Is.Empty);
                Assert.That(GetClientContainer(cEntManager, cContainerSys, ownerNet, OtherContainerId).ContainedEntities, Is.Empty);
                Assert.That(cContainerSys.PvsDetachedEntities, Is.Empty);
                AssertPendingContainerState(cClientEntManager, itemNet, cOwner);
            });

            await server.WaitAssertion(() =>
            {
                sVisibilitySys.RemoveLayer(item, HiddenVisibilityLayer);
            });

            await RunTicksSync(server, client, 10);

            await client.WaitAssertion(() =>
            {
                var cItem = GetClientEntity(cEntManager, itemNet);
                var firstContainer = GetClientContainer(cEntManager, cContainerSys, ownerNet);
                var secondContainer = GetClientContainer(cEntManager, cContainerSys, ownerNet, OtherContainerId);
                Assert.That(firstContainer.ContainedEntities, Is.Empty);
                Assert.That(secondContainer.ContainedEntities, Has.Member(cItem));
                Assert.That(cContainerSys.PvsDetachedEntities, Is.Empty);
                Assert.That(cClientEntManager.PendingNetEntityStates.ContainsKey(itemNet), Is.False);
            });
        }

        /// <summary>
        /// Tests that PVS-detached contained entities are restored when their transform re-enters PVS.
        /// </summary>
        [Test]
        public async Task ContainerPvsDetachedEntityRestoresOnReEntry()
        {
            await using var pair = await StartConnectedPair();
            var server = pair.Server;
            var client = pair.Client;

            await RunTicksSync(server, client, 10);

            var cEntManager = client.ResolveDependency<IEntityManager>();
            var cContainerSys = cEntManager.System<ContainerSystem>();

            var sEntManager = server.ResolveDependency<IEntityManager>();
            var sPlayerManager = server.ResolveDependency<IPlayerManager>();
            var sContainerSys = sEntManager.System<SharedContainerSystem>();
            var sMetadataSys = sEntManager.System<MetaDataSystem>();
            var sVisibilitySys = sEntManager.System<SharedVisibilitySystem>();

            var (owner, mapPos) = await SpawnContainerOwner(server, sEntManager, sPlayerManager, sContainerSys, sMetadataSys);
            var ownerNet = sEntManager.GetNetEntity(owner);

            await RunTicksSync(server, client, 10);

            EntityUid item = default;
            NetEntity itemNet = default;
            await server.WaitAssertion(() =>
            {
                item = sEntManager.SpawnEntity(null, mapPos);
                itemNet = sEntManager.GetNetEntity(item);
                sMetadataSys.SetEntityName(item, "Item");
                Assert.That(sContainerSys.Insert(item, sContainerSys.GetContainer(owner, ContainerId)));
            });

            await RunTicksSync(server, client, 10);

            EntityUid cItem = default;
            await client.WaitAssertion(() =>
            {
                cItem = GetClientEntity(cEntManager, itemNet);
                var container = GetClientContainer(cEntManager, cContainerSys, ownerNet);
                Assert.That(container.ContainedEntities, Has.Member(cItem));
                Assert.That(cEntManager.GetComponent<MetaDataComponent>(cItem).Flags & MetaDataFlags.Detached, Is.EqualTo(MetaDataFlags.None));
            });

            await server.WaitAssertion(() =>
            {
                sVisibilitySys.AddLayer(item, HiddenVisibilityLayer);
            });

            await RunTicksSync(server, client, 10);

            await client.WaitAssertion(() =>
            {
                var container = GetClientContainer(cEntManager, cContainerSys, ownerNet);
                Assert.That(container.ContainedEntities, Is.Empty);
                Assert.That(container.PvsDetachedEntities, Has.Member(itemNet));
                Assert.That(cContainerSys.PvsDetachedEntities.TryGetValue(itemNet, out var detachedContainer), Is.True);
                Assert.That(detachedContainer, Is.SameAs(container));
                Assert.That(cEntManager.GetComponent<MetaDataComponent>(cItem).Flags & MetaDataFlags.Detached, Is.EqualTo(MetaDataFlags.Detached));
            });

            await server.WaitAssertion(() =>
            {
                sVisibilitySys.RemoveLayer(item, HiddenVisibilityLayer);
            });

            await RunTicksSync(server, client, 10);

            await client.WaitAssertion(() =>
            {
                var container = GetClientContainer(cEntManager, cContainerSys, ownerNet);
                Assert.That(container.ContainedEntities, Has.Member(cItem));
                Assert.That(container.PvsDetachedEntities, Is.Empty);
                Assert.That(cContainerSys.PvsDetachedEntities, Is.Empty);
                Assert.That(cEntManager.GetComponent<MetaDataComponent>(cItem).Flags & MetaDataFlags.Detached, Is.EqualTo(MetaDataFlags.None));
            });
        }

        /// <summary>
        /// Tests that pending container-state reapplication safely ignores a deleted container owner.
        /// </summary>
        [Test]
        public async Task ContainerStateMissingEntityOwnerDeletedBeforeSpawn()
        {
            await using var pair = await StartConnectedPair();
            var server = pair.Server;
            var client = pair.Client;

            await RunTicksSync(server, client, 10);

            var cEntManager = client.ResolveDependency<IEntityManager>();
            var cClientEntManager = (ClientEntityManager) cEntManager;
            var cContainerSys = cEntManager.System<ContainerSystem>();

            var sEntManager = server.ResolveDependency<IEntityManager>();
            var sPlayerManager = server.ResolveDependency<IPlayerManager>();
            var sContainerSys = sEntManager.System<SharedContainerSystem>();
            var sMetadataSys = sEntManager.System<MetaDataSystem>();
            var sVisibilitySys = sEntManager.System<SharedVisibilitySystem>();

            var (owner, mapPos) = await SpawnContainerOwner(server, sEntManager, sPlayerManager, sContainerSys, sMetadataSys);
            var ownerNet = sEntManager.GetNetEntity(owner);

            await RunTicksSync(server, client, 10);
            var cOwner = GetClientEntity(cEntManager, ownerNet);

            EntityUid item = default;
            NetEntity itemNet = default;
            await server.WaitAssertion(() =>
            {
                item = sEntManager.SpawnEntity(null, mapPos);
                itemNet = sEntManager.GetNetEntity(item);
                sMetadataSys.SetEntityName(item, "Item");
                Assert.That(sContainerSys.Insert(item, sContainerSys.GetContainer(owner, ContainerId)));
                sVisibilitySys.AddLayer(item, HiddenVisibilityLayer);
            });

            await RunTicksSync(server, client, 10);

            await client.WaitAssertion(() =>
            {
                Assert.That(cEntManager.TryGetEntity(itemNet, out _), Is.False);
                Assert.That(GetClientContainer(cEntManager, cContainerSys, ownerNet).ContainedEntities, Is.Empty);
                Assert.That(cContainerSys.PvsDetachedEntities, Is.Empty);
                AssertPendingContainerState(cClientEntManager, itemNet, cOwner);
            });

            await server.WaitAssertion(() =>
            {
                var viewer = sEntManager.SpawnEntity(null, mapPos);
                sEntManager.AddComponent<EyeComponent>(viewer);
                server.PlayerMan.SetAttachedEntity(sPlayerManager.Sessions.First(), viewer);
            });

            await RunTicksSync(server, client, 10);

            await server.WaitAssertion(() =>
            {
                sContainerSys.Remove(item, sContainerSys.GetContainer(owner, ContainerId), force: true);
                sEntManager.DeleteEntity(owner);
            });

            await RunTicksSync(server, client, 10);

            await client.WaitAssertion(() =>
            {
                Assert.That(cEntManager.EntityExists(cOwner), Is.False);
                Assert.That(cContainerSys.PvsDetachedEntities, Is.Empty);
            });

            await server.WaitAssertion(() =>
            {
                sVisibilitySys.RemoveLayer(item, HiddenVisibilityLayer);
            });

            await RunTicksSync(server, client, 10);

            await client.WaitAssertion(() =>
            {
                var cItem = GetClientEntity(cEntManager, itemNet);
                Assert.That(cEntManager.GetComponent<MetaDataComponent>(cItem).Flags & MetaDataFlags.InContainer, Is.EqualTo(MetaDataFlags.None));
                Assert.That(cContainerSys.PvsDetachedEntities, Is.Empty);
                Assert.That(cClientEntManager.PendingNetEntityStates.ContainsKey(itemNet), Is.False);
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
