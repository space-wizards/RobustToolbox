using NUnit.Framework;
using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Timing;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Reflection;
using Robust.Shared.Timing;

namespace Robust.UnitTesting.Shared.GameObjects
{
    internal sealed partial class ContainerTestsForceRemoval: RobustIntegrationTest
    {
        /// <summary>
        /// Creates a situation where an unremovable item is created and tries to remove it from the container
        /// originally based on ContainerTests.TestContainerExpectedEntityDeleted (now deleted).
        /// <br/>
        /// could probably cut down on a lot of the boilerplate here
        /// </summary>
        [Test]
        public async Task TestForceRemovalOfUnremovableItem()
        {
            // Start a server and a client, registering a system to provide functionality for making an item unremovable from a container by normal means
            await using var pair = await StartConnectedPair(
                UnremovableFromContainerIntegrationOptions(new ServerIntegrationOptions()),
                UnremovableFromContainerIntegrationOptions(new ClientIntegrationOptions())
            );
            var server = pair.Server;
            var client = pair.Client;

            // give the server and client a moment to fully initialize (so we can obtain systems)
            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());
            await RunTicksSync(server, client, 5);

            var cEntManager = client.ResolveDependency<IEntityManager>();
            var cContainerSys = cEntManager.System<ContainerSystem>();
            var clientTime = client.ResolveDependency<IClientGameTiming>();

            var sEntManager = server.ResolveDependency<IEntityManager>();
            var sPlayerManager = server.ResolveDependency<IPlayerManager>();
            var sContainerSys = sEntManager.System<SharedContainerSystem>();
            var sMetadataSys = sEntManager.System<MetaDataSystem>();
            var serverTime = server.ResolveDependency<IGameTiming>();

            var containerEntName = "ContainerEnt";
            var containerId = "testContainer";
            var itemName = "TheItem";

            // setup
            MapId mapId;
            var mapPos = MapCoordinates.Nullspace;

            EntityUid sContainerUid = default!;
            EntityUid sItemUid = default!;
            NetEntity itemNetEnt = default;

            await server.WaitAssertion(() =>
            {
                sEntManager.System<SharedMapSystem>().CreateMap(out mapId);
                mapPos = new MapCoordinates(new Vector2(0, 0), mapId);
    
                sContainerUid = sEntManager.Spawn(null, mapPos);
                sMetadataSys.SetEntityName(sContainerUid, containerEntName);
                sContainerSys.EnsureContainer<Container>(sContainerUid, containerId);
    
                // Setup PVS
                sEntManager.AddComponent<EyeComponent>(sContainerUid);
                var player = sPlayerManager.Sessions.First();
                server.PlayerMan.SetAttachedEntity(player, sContainerUid);
                sPlayerManager.JoinGame(player);
            });

            await RunTicksSync(server, client, 5);

            await server.WaitAssertion(() =>
            {
                sItemUid = sEntManager.Spawn(null, mapPos);
                itemNetEnt = sEntManager.GetNetEntity(sItemUid);
                sMetadataSys.SetEntityName(sItemUid, itemName);
                // make the item unremovable, and put it in the container.
                sEntManager.AddComponent<UnremovableFromContainerComponent>(sItemUid);
                var container = sContainerSys.GetContainer(sContainerUid, containerId);
                Assume.That(sContainerSys.Insert(sItemUid, container), Is.True, "Unable to insert the item into the container via sContainerSys.Insert, tests are invalid.");
            });

            // let the client catch up to the server
            await server.WaitRunTicks(1);
            while (clientTime.LastRealTick < serverTime.CurTick - 1)
            {
                await client.WaitRunTicks(1);
            }

            // obtain client uids/components
            var cItemUid = cEntManager.GetEntity(itemNetEnt);
            var cContainerUid = cEntManager.GetEntity(sEntManager.GetNetEntity(sContainerUid));
            if (!cEntManager.TryGetComponent<ContainerManagerComponent>(cContainerUid, out var cContainerManagerComp))
            {
                Assert.Fail("Unable to get client's ContainerManagerComponent, cannot make sure the client is working correctly");
                return;
            }
            var cBaseContainer = cContainerSys.GetContainer(cContainerUid, containerId, cContainerManagerComp);

            // clientside assumptions (make sure the item exists and is in the container)
            await client.WaitAssertion(() =>
            {
                Assume.That(cEntManager.EntityExists(cItemUid), Is.True, "Item does not exist in client :(");
                Assume.That(cContainerSys.IsEntityInContainer(cItemUid), Is.True, "Item is not in a container in client :(");
                Assume.That(cContainerSys.TryGetContainingContainer(cItemUid, out _), Is.True, "Item does not have containing container in client :(");
                Assume.That(cBaseContainer.ContainedEntities.Count, Is.EqualTo(1), "Clientside BaseContainer did not have any contained entities! (ContainedEntities.Count != 1)");
                Assume.That(cBaseContainer.Contains(cItemUid), Is.True, "Clientside BaseContainer does not contain the item!");
            });

            await RunTicksSync(server, client, 1);

            // attempt removing the unremovable item from the container (it should remain in the container)
            await server.WaitAssertion(() =>
            {
                Assume.That(sContainerSys.TryRemoveFromContainer(sItemUid, force: false), Is.False, "Unremovable item was removed from container without being forced! (TryRemoveFromContainer should have returned false)");
                Assume.That(sContainerSys.IsEntityInContainer(sItemUid), Is.True, "Unremovable item still in container after unforced removal (it should still be in the container)");
            });

            await RunTicksSync(server, client, 1);

            // just making sure the unforced removal didn't remove the item from the container on the client side
            await client.WaitAssertion(() =>
            {
                Assume.That(cBaseContainer.Contains(cItemUid), Is.True, "Clientside BaseContainer does not contain the item after an unforced removal (it should still be in the container!)");
            });

            await RunTicksSync(server, client, 1);

            // and now, we finally forcibly remove the item from the container.
            await server.WaitAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(sContainerSys.TryRemoveFromContainer(sItemUid, force: true), Is.True, "Unremovable item wasn't removed from container despite being forced out! (TryRemoveFromContainer should have returned true)");
                    Assert.That(sContainerSys.IsEntityInContainer(sItemUid), Is.False, "Unremovable item still in container after forced removal (it should not be in the container)");
                }
            });

            await RunTicksSync(server, client, 1);

            // make sure that it was removed from the container on the client side as well
            await client.WaitAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(cContainerSys.IsEntityInContainer(cItemUid), Is.False, "The item was still in a container on the client after forced removal! (IsEntityInContainer)");
                    Assert.That(cBaseContainer.Contains(cItemUid), Is.False, "The item was still in the clientside BaseContainer after forced removal! (cBaseContaner.Contains returned true)");
                    Assert.That(cBaseContainer.ContainedEntities.Count, Is.EqualTo(0), "There was still an item in the container in the client after forced removal! (ContainedEntities.Count != 0)");
                    Assert.That(cBaseContainer.PvsDetachedEntities.Count, Is.EqualTo(0), "There was still an item in the client's PvsDetachedEntities after forced removal! (PvsDetachedEntities.Count != 0)");
                    Assert.That(cContainerSys.PvsDetachedEntities.ContainsKey(itemNetEnt), Is.False, "The item is still registered as a PVS-detached contained entity on the client despite no longer being a contained entity!");
                }
            });

            await DisconnectClient(server, client);
        }

        /// <summary>
        /// Sets up an <see cref="RobustIntegrationTest.IntegrationOptions">IntegrationOptions</see> instance
        /// with the <see cref="UnremovableFromContainerSystem"/>/<see cref="UnremovableFromContainerComponent"/> system set up.
        /// </summary>
        /// <param name="intOptions">IntegrationOptions instance to modify (construct a client or a server instance)</param>
        /// <returns><paramref name="intOptions"/> but set up with the UnremovableFromContainer stuff ready to go</returns>
        private T UnremovableFromContainerIntegrationOptions<T>(T intOptions) where T : IntegrationOptions
        {
            intOptions.BeforeRegisterComponents += () =>
            {
                var fact = IoCManager.Resolve<IComponentFactory>();
                fact.RegisterClass<UnremovableFromContainerComponent>();
            };
            intOptions.BeforeStart += () =>
            {
                var sysMan = IoCManager.Resolve<IEntitySystemManager>();
                sysMan.LoadExtraSystemType<UnremovableFromContainerSystem>();
            };
            return intOptions;
        }
        
        /// <summary>
        /// component which prevents an entity from being removed from a container unless forced
        /// </summary>
        [Reflect(discoverable:false)]
#pragma warning disable RA0003
        private partial class UnremovableFromContainerComponent : Component { }
#pragma warning restore RA0003

        /// <summary>
        /// prevents entities with UnremovableFromContainerComponent from getting removed from containers
        /// </summary>
        [Reflect(discoverable:false)]
        private sealed class UnremovableFromContainerSystem : EntitySystem
        {
            public override void Initialize()
            {
                SubscribeLocalEvent<ContainerGettingRemovedAttemptEvent>(OnContainerGettingRemovedAttemptEvent);
            }

            private void OnContainerGettingRemovedAttemptEvent(ContainerGettingRemovedAttemptEvent ev)
            {
                ev.Cancel();
            }
        }
    }
}

