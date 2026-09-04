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
    
    [TestFixture]
    internal sealed partial class ContainerTestsForceRemoval: RobustIntegrationTest
    {
        /// <summary>
        /// Creates a situation where an unremovable item is created and tries to remove it from the container
        /// heavily based on <see cref="Robust.UnitTesting.Shared.GameObjects.ContainerTests.TestContainerExpectedEntityDeleted">TestContainerExpectedEntityDeleted</see>.
        /// <br/>
        /// could probably cut down on a lot of the boilerplate here
        /// </summary>
        [Test]
        public async Task TestForceRemovalOfUnremovableItem()
        {
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
                // make the item unremovable
                sEntManager.AddComponent<UnremovableFromContainerComponent>(sItemUid);
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
                Assume.That(sEntManager.EntityExists(sItemUid), Is.True, "Item does not exist :(");
                
                Assume.That(sContainerSys.TryGetContainingContainer(sItemUid, out _), Is.True, "Item was not in a container :(");
                
                Assert.That(sContainerSys.TryRemoveFromContainer(sItemUid, force: false), Is.False, "Unremovable item was removed from container without being forced!");
                Assert.That(sContainerSys.IsEntityInContainer(sItemUid), Is.True, "Unremovable item still in container after unforced removal");
                
                Assert.That(sContainerSys.TryRemoveFromContainer(sItemUid, force: true), Is.True, "Unremovable item wasn't removed from container despite being forced!");
                Assert.That(sContainerSys.IsEntityInContainer(sItemUid), Is.False, "Unremovable item still in container after forced removal :(");
    
                //sEntManager.DeleteEntity(sItemUid);
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
        /// component which prevents the entity from being removed from the container unless forced
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

