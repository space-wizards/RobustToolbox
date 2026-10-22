using NUnit.Framework;
using Robust.UnitTesting;
using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Timing;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Reflection;
using Robust.Shared.Timing;

namespace Robust.UnitTesting.Shared.GameObjects
{
    internal sealed partial class ContainerTestsForceRemoval: RobustIntegrationTest
    {
        /// <summary>
        /// Creates a situation where an unremovable item is created and tries to remove it from the container
        /// heavily based on ContainerTests.TestContainerExpectedEntityDeleted (now deleted).
        /// <br/>
        /// could probably cut down on a lot of the boilerplate here
        /// </summary>
        [Test]
        public async Task TestForceRemovalOfUnremovableItem()
        {
            // this registers a system to provide the "unremovable item" functionality
            var options = new ServerIntegrationOptions();
            options.Pool = false;
            options.BeforeRegisterComponents += () =>
            {
                var fact = IoCManager.Resolve<IComponentFactory>();
                fact.RegisterClass<UnremovableFromContainerComponent>();
            };
            options.BeforeStart += () =>
            {
                var sysMan = IoCManager.Resolve<IEntitySystemManager>();
                sysMan.LoadExtraSystemType<UnremovableFromContainerSystem>();
            };
            
            var server = StartServer(options);
            var client = StartClient();
            
            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());
    
            var cEntManager = client.ResolveDependency<IEntityManager>();
            var clientTime = client.ResolveDependency<IClientGameTiming>();
            var clientNetManager = client.ResolveDependency<IClientNetManager>();
    
            var sEntManager = server.ResolveDependency<IEntityManager>();
            var sPlayerManager = server.ResolveDependency<IPlayerManager>();
            var serverTime = server.ResolveDependency<IGameTiming>();

            Assert.DoesNotThrow(() => client.SetConnectTarget(server));
            await client.WaitPost(() =>
            {
                clientNetManager.ClientConnect(null!, 0, null!);
            });
    
            await RunTicksSync(server, client, 10);
            
            var containerEntName = "ContainerEnt";
            var containerId = "TheContainer";
            var itemName = "TheItem";
            // Setup
            MapId mapId;
            var mapPos = MapCoordinates.Nullspace;
    
            EntityUid sContainerUid = default!;
            EntityUid sItemUid = default!;
            NetEntity itemNetEnt = default;
    
            var cContainerSys = cEntManager.System<ContainerSystem>();
            var sContainerSys = sEntManager.System<SharedContainerSystem>();
            var sMetadataSys = sEntManager.System<MetaDataSystem>();
    
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
    
            await RunTicksSync(server, client, 10);
    
            await server.WaitAssertion(() =>
            {
                sItemUid = sEntManager.Spawn(null, mapPos);
                itemNetEnt = sEntManager.GetNetEntity(sItemUid);
                sMetadataSys.SetEntityName(sItemUid, itemName);
                // make the item unremovable
                sEntManager.AddComponent<UnremovableFromContainerComponent>(sItemUid);
                var container = sContainerSys.GetContainer(sContainerUid, containerId);
                Assume.That(sContainerSys.Insert(sItemUid, container), Is.True, "Unable to insert the item into the container via sContainerSys.Insert, tests are invalid.");
    
                // Modify visibility layer so that the item does not get sent to the player
                //sEntManager.System<SharedVisibilitySystem>().AddLayer(sItemUid, 10 );
            });
    
            // let the client catch up to the server
            await server.WaitRunTicks(1);
            while (clientTime.LastRealTick < serverTime.CurTick - 1)
            {
                await client.WaitRunTicks(1);
            }
    
            // obtain client uids/components for later
            var cItemUid = cEntManager.GetEntity(itemNetEnt);
            var cContainerUid = cEntManager.GetEntity(sEntManager.GetNetEntity(sContainerUid));
            if (!cEntManager.TryGetComponent<ContainerManagerComponent>(cContainerUid, out var cContainerManagerComp))
            {
                Assert.Fail("Unable to get client's ContainerManagerComponent, cannot perform subsequent tests");
                return;
            }
            var cBaseContainer = cContainerSys.GetContainer(cContainerUid, containerId, cContainerManagerComp);

            await client.WaitAssertion(() =>
            {
                Assume.That(cEntManager.EntityExists(cItemUid), Is.True, "Item does not exist in client :(");
                Assume.That(cContainerSys.IsEntityInContainer(cItemUid), Is.True, "Item is not in a container in client :(");
                Assume.That(cContainerSys.TryGetContainingContainer(cItemUid, out _), Is.True, "Item does not have containing container in client :(");
                Assume.That(cBaseContainer.ContainedEntities.Count, Is.EqualTo(1), "Clientside BaseContainer did not have any contained entities! (ContainedEntities.Count != 1)");
                Assume.That(cBaseContainer.Contains(cItemUid), Is.True, "Clientside BaseContainer does not contain the item!");
            });
    
            await server.WaitAssertion(() =>
            {
                Assume.That(sEntManager.EntityExists(sItemUid), Is.True, "Item does not exist on server :(");
                Assume.That(sContainerSys.TryGetContainingContainer(sItemUid, out _), Is.True, "Item does not have containing container on server :(");
                
                Assert.That(sContainerSys.TryRemoveFromContainer(sItemUid, force: false), Is.False, "Unremovable item was removed from container without being forced! (TryRemoveFromContainer should have returned false)");
                Assert.That(sContainerSys.IsEntityInContainer(sItemUid), Is.True, "Unremovable item still in container after unforced removal (it should still be in the container)");
                
                Assert.That(sContainerSys.TryRemoveFromContainer(sItemUid, force: true), Is.True, "Unremovable item wasn't removed from container despite being forced out! (TryRemoveFromContainer should have returned true)");
                Assert.That(sContainerSys.IsEntityInContainer(sItemUid), Is.False, "Unremovable item still in container after forced removal (it should not be in the container)");
            });
    
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(4);
    
            await client.WaitAssertion(() =>
            {
                Assert.That(cContainerSys.IsEntityInContainer(cItemUid), Is.False, "The item was still in a container on the client after forced removal! (IsEntityInContainer)");
                Assert.That(cBaseContainer.Contains(cItemUid), Is.False, "The item was still in the clientside BaseContainer after forced removal! (cBaseContaner.Contains returned true)");
                Assert.That(cBaseContainer.ContainedEntities.Count, Is.EqualTo(0), "There was still an item in the container in the client after forced removal! (ContainedEntities.Count != 0)");
                Assert.That(cBaseContainer.PvsDetachedEntities.Count, Is.EqualTo(0), "There was still an item in the client's PvsDetachedEntities after forced removal! (PvsDetachedEntities.Count != 0)");
    
                Assert.That(cContainerSys.PvsDetachedEntities.ContainsKey(itemNetEnt), Is.False, "The item is still registered as a PVS-detached contained entity on the client despite no longer being a contained entity!");
            });
    
            await DisconnectClient(server, client);
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

