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

        private IComponentFactory _components = default!;
        private PrototypeManager _prototypes = default!;
        private IMapManager _maps = default!;
        private IEntityManager _entities = default!;

        [OneTimeSetUp]
        public void Setup()
        {
            _components = IoCManager.Resolve<IComponentFactory>();
            _components.RegisterClass<HotReloadTestOneComponent>();
            _components.RegisterClass<HotReloadTestTwoComponent>();
            _components.GenerateNetIds();

            IoCManager.Resolve<ISerializationManager>().Initialize();
            _prototypes = (PrototypeManager) IoCManager.Resolve<IPrototypeManager>();
            _prototypes.RegisterType(typeof(EntityPrototype));
            _prototypes.LoadString(InitialPrototypes);
            _prototypes.Resync();

            _maps = IoCManager.Resolve<IMapManager>();
            _entities = IoCManager.Resolve<IEntityManager>();
        }

        [Test]
        public void TestHotReload()
        {
            _maps.CreateNewMapEntity(new MapId(0));
            var entity = _entities.SpawnEntity(DummyId, MapCoordinates.Nullspace);
            var entityComponent = IoCManager.Resolve<IEntityManager>().GetComponent<HotReloadTestOneComponent>(entity);

            Assert.That(entityComponent.Value, Is.EqualTo(5));
            Assert.False(IoCManager.Resolve<IEntityManager>().HasComponent<HotReloadTestTwoComponent>(entity));

            var reloaded = false;
            _prototypes.PrototypesReloaded += _ => reloaded = true;

            _prototypes.ReloadPrototypes(new List<IPrototype>());

            Assert.True(reloaded);
            reloaded = false;

            Assert.That(entityComponent.Value, Is.EqualTo(5));
            Assert.False(IoCManager.Resolve<IEntityManager>().HasComponent<HotReloadTestTwoComponent>(entity));

            var changedPrototypes = _prototypes.LoadString(ReloadedPrototypes, true);
            _prototypes.ReloadPrototypes(changedPrototypes);

            Assert.True(reloaded);
            reloaded = false;

            // Existing component values are not modified in the current implementation
            Assert.That(entityComponent.Value, Is.EqualTo(5));

            // New components are added
            Assert.True(IoCManager.Resolve<IEntityManager>().HasComponent<HotReloadTestTwoComponent>(entity));

            changedPrototypes = _prototypes.LoadString(InitialPrototypes, true);
            _prototypes.ReloadPrototypes(changedPrototypes);

            Assert.True(reloaded);
            reloaded = false;

            // Existing component values are not modified in the current implementation
            Assert.That(entityComponent.Value, Is.EqualTo(5));

            // Old components are removed
            Assert.False(IoCManager.Resolve<IEntityManager>().HasComponent<HotReloadTestTwoComponent>(entity));
        }
    }

    public sealed class HotReloadTestOneComponent : Component
    {
        [DataField("value")]
        public int Value { get; }
    }

    public sealed class HotReloadTestTwoComponent : Component
    {
    }
}
