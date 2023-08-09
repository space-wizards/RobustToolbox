using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.GameObjects;

public sealed partial class DeferredEntityDeletionTest : RobustIntegrationTest
{
    // This test ensures that deferred deletion can be used while handling events without issue, and that deleting an
    // entity after deferring component removal doesn't cause any issues.

    [Test]
    public async Task TestDeferredEntityDeletion()
    {
        var options = new ServerIntegrationOptions();
        options.Pool = false;
        options.BeforeRegisterComponents += () =>
        {
            var fact = IoCManager.Resolve<IComponentFactory>();
            fact.RegisterClass<DeferredDeletionTestComponent>();
            fact.RegisterClass<OtherDeferredDeletionTestComponent>();
        };
        options.BeforeStart += () =>
        {
            var sysMan = IoCManager.Resolve<IEntitySystemManager>();
            sysMan.LoadExtraSystemType<DeferredDeletionTestSystem>();
            sysMan.LoadExtraSystemType<OtherDeferredDeletionTestSystem>();
        };

        var server = StartServer(options);
        await server.WaitIdleAsync();

        EntityUid uid1 = default, uid2 = default, uid3 = default;
        DeferredDeletionTestComponent comp1 = default!, comp2 = default!, comp3 = default!;
        IEntityManager entMan = default!;
        
        await server.WaitAssertion(() =>
        {
            var mapMan = IoCManager.Resolve<IMapManager>();
            entMan = IoCManager.Resolve<IEntityManager>();
            var sys = entMan.EntitySysManager.GetEntitySystem<DeferredDeletionTestSystem>();

            uid1 = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
            uid2 = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
            uid3 = entMan.SpawnEntity(null, MapCoordinates.Nullspace);

            comp1 = entMan.AddComponent<DeferredDeletionTestComponent>(uid1);
            comp2 = entMan.AddComponent<DeferredDeletionTestComponent>(uid2);
            comp3 =entMan.AddComponent<DeferredDeletionTestComponent>(uid3);

            entMan.AddComponent<OtherDeferredDeletionTestComponent>(uid1);
            entMan.AddComponent<OtherDeferredDeletionTestComponent>(uid2);
            entMan.AddComponent<OtherDeferredDeletionTestComponent>(uid3);
        });

        await server.WaitRunTicks(1);

        // first: test that deferring deletion while handling events doesn't cause issues
        await server.WaitAssertion(() =>
        {
            Assert.That(comp1.Running);
            var ev = new DeferredDeletionTestEvent();
            entMan.EventBus.RaiseLocalEvent(uid1, ev);
            Assert.That(comp1.LifeStage == ComponentLifeStage.Stopped);
        });

        await server.WaitRunTicks(1);
        Assert.That(comp1.LifeStage == ComponentLifeStage.Deleted);

        // next check that entity deletion doesn't cause issues:
        await server.WaitAssertion(() =>
        {
            var ev = new DeferredDeletionTestEvent();
            entMan.EventBus.RaiseLocalEvent(uid2, ev);
            entMan.EventBus.RaiseLocalEvent(uid3, ev);
            entMan.DeleteEntity(uid2);
            entMan.QueueDeleteEntity(uid3);
            Assert.That(entMan.Deleted(uid2));
            Assert.That(!entMan.Deleted(uid3));
            Assert.That(comp2.LifeStage == ComponentLifeStage.Deleted);
            Assert.That(comp3.LifeStage == ComponentLifeStage.Stopped);
        });
        
        await server.WaitRunTicks(1);
        Assert.That(comp3.LifeStage == ComponentLifeStage.Deleted);
        Assert.That(entMan.Deleted(uid3));
        await server.WaitIdleAsync();
    }

    private sealed class DeferredDeletionTestSystem : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<DeferredDeletionTestComponent, DeferredDeletionTestEvent>(OnTestEvent);
        }

        private void OnTestEvent(EntityUid uid, DeferredDeletionTestComponent component, DeferredDeletionTestEvent args)
        {
            // remove both this component, and some other component that this entity has that also subscribes to this event.
            RemCompDeferred<DeferredDeletionTestComponent>(uid);
            RemCompDeferred<OtherDeferredDeletionTestComponent>(uid);
        }
    }

    private sealed class OtherDeferredDeletionTestSystem : EntitySystem
    {
        public override void Initialize() => SubscribeLocalEvent<OtherDeferredDeletionTestComponent, DeferredDeletionTestEvent>(OnTestEvent);

        private void OnTestEvent(EntityUid uid, OtherDeferredDeletionTestComponent component, DeferredDeletionTestEvent args)
        {
            // remove both this component, and some other component that this entity has that also subscribes to this event.
            RemCompDeferred<DeferredDeletionTestComponent>(uid);
            RemCompDeferred<OtherDeferredDeletionTestComponent>(uid);
        }
    }

    [RegisterComponent]
    private sealed class DeferredDeletionTestComponent : Component
    {
    }

    private sealed class OtherDeferredDeletionTestComponent : Component
    {
    }

    private sealed class DeferredDeletionTestEvent
    {
    }
}
