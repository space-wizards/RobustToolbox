#if TOOLS
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.UnitTesting.Shared.Prototypes
{
    [TestFixture]
    public sealed class HotReloadTest : RobustUnitTest
    {
        private const string DummyId = "Dummy";
        public const string HotReloadTestComponentOneId = "HotReloadTestOne";
        public const string HotReloadTestComponentTwoId = "HotReloadTestTwo";

        private static readonly string InitialPrototypes = $@"
- type: entity
  id: {DummyId}
  components:
  - type: {HotReloadTestComponentOneId}
    value: 5";

        private static readonly string ReloadedPrototypes = $@"
- type: entity
  id: {DummyId}
  components:
  - type: {HotReloadTestComponentOneId}
    value: 10
  - type: {HotReloadTestComponentTwoId}";

        private PrototypeManager _prototypes = default!;
        private IMapManager _maps = default!;
        private IEntityManager _entities = default!;

        protected override Type[]? ExtraComponents => new[] {typeof(HotReloadTestOneComponent), typeof(HotReloadTestTwoComponent)};

        [OneTimeSetUp]
        public void Setup()
        {
            IoCManager.Resolve<ISerializationManager>().Initialize();
            _prototypes = (PrototypeManager) IoCManager.Resolve<IPrototypeManager>();
            _prototypes.RegisterKind(typeof(EntityPrototype), typeof(EntityCategoryPrototype));
            _prototypes.LoadString(InitialPrototypes);
            _prototypes.ResolveResults();

            _maps = IoCManager.Resolve<IMapManager>();
            _entities = IoCManager.Resolve<IEntityManager>();
        }

        [Test]
        public void TestHotReload()
        {
            IoCManager.Resolve<IEntityManager>().System<SharedMapSystem>().CreateMap(out var id);
            var entity = _entities.SpawnEntity(DummyId, new MapCoordinates(default, id));
            var entityComponent = IoCManager.Resolve<IEntityManager>().GetComponent<HotReloadTestOneComponent>(entity);

            Assert.That(entityComponent.Value, Is.EqualTo(5));
            Assert.That(IoCManager.Resolve<IEntityManager>().HasComponent<HotReloadTestTwoComponent>(entity), Is.False);

            var reloaded = false;
            _prototypes.PrototypesReloaded += _ => reloaded = true;

            _prototypes.ReloadPrototypes(new Dictionary<Type, HashSet<string>>());

            Assert.That(reloaded);
            reloaded = false;

            Assert.That(entityComponent.Value, Is.EqualTo(5));
            Assert.That(IoCManager.Resolve<IEntityManager>().HasComponent<HotReloadTestTwoComponent>(entity), Is.False);

            var changedPrototypes = new Dictionary<Type, HashSet<string>>();
            _prototypes.LoadString(ReloadedPrototypes, true, changedPrototypes);
            _prototypes.ReloadPrototypes(changedPrototypes);

            Assert.That(reloaded);
            reloaded = false;

            // Existing component values are not modified in the current implementation
            Assert.That(entityComponent.Value, Is.EqualTo(5));

            // New components are added
            Assert.That(IoCManager.Resolve<IEntityManager>().HasComponent<HotReloadTestTwoComponent>(entity));

            changedPrototypes = new Dictionary<Type, HashSet<string>>();
            _prototypes.LoadString(InitialPrototypes, true, changedPrototypes);
            _prototypes.ReloadPrototypes(changedPrototypes);

            Assert.That(reloaded);
            reloaded = false;

            // Existing component values are not modified in the current implementation
            Assert.That(entityComponent.Value, Is.EqualTo(5));

            // Old components are removed
            Assert.That(IoCManager.Resolve<IEntityManager>().HasComponent<HotReloadTestTwoComponent>(entity), Is.False);
        }
    }

    public sealed partial class HotReloadTestOneComponent : Component
    {
        [DataField("value")]
        public int Value { get; private set; }
    }

    public sealed partial class HotReloadTestTwoComponent : Component
    {
    }
}
#endif
