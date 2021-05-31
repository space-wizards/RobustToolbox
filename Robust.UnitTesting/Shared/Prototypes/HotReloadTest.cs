using System.Collections.Generic;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Robust.UnitTesting.Shared.Prototypes
{
    [TestFixture]
    public class HotReloadTest : RobustUnitTest
    {
        private const string DummyId = "Dummy";
        public const string HotReloadTestComponentOneId = "HotReloadTestOne";
        public const string HotReloadTestComponentTwoId = "HotReloadTestTwo";
        public const string HotReloadPrototypeOneId = "HotReloadPrototypeOne";

        private static readonly string InitialEntityString = $@"
- type: entity
  id: {DummyId}
  components:
  - type: {HotReloadTestComponentOneId}
    value: 5";

        private static readonly string ReloadedPrototypeStringOne = $@"
- type: hotReload
  id: {HotReloadPrototypeOneId}
  value: A";

        private static readonly string ReloadedEntityStringTwo = $@"
- type: entity
  id: {DummyId}
  components:
  - type: {HotReloadTestComponentOneId}
    value: 10
    prototype: {HotReloadPrototypeOneId}
  - type: {HotReloadTestComponentTwoId}";

        private static readonly string ReloadedPrototypeStringThree = $@"
- type: hotReload
  id: {HotReloadPrototypeOneId}
  value: B";

        private IComponentFactory _components = default!;
        private PrototypeManager _prototypes = default!;
        private IMapManager _maps = default!;
        private IEntityManager _entities = default!;

        [OneTimeSetUp]
        public void Setup()
        {
            _components = IoCManager.Resolve<IComponentFactory>();
            _components.RegisterClass<HotReloadTestComponentOne>();
            _components.RegisterClass<HotReloadTestComponentTwo>();

            IoCManager.Resolve<ISerializationManager>().Initialize();
            _prototypes = (PrototypeManager) IoCManager.Resolve<IPrototypeManager>();
            _prototypes.LoadString(InitialEntityString);
            _prototypes.Resync();

            _maps = IoCManager.Resolve<IMapManager>();
            _entities = IoCManager.Resolve<IEntityManager>();
        }

        private void AssertReload(string prototypes)
        {
            var reloaded = false;
            _prototypes.PrototypesReloaded += _ => reloaded = true;

            var changedPrototypes = _prototypes.LoadString(prototypes, true);
            _prototypes.ReloadPrototypes(changedPrototypes);

            Assert.True(reloaded);
        }

        [Test]
        public void TestHotReload()
        {
            // Initial string already loaded
            _maps.CreateNewMapEntity(new MapId(0));
            var entity = _entities.SpawnEntity(DummyId, MapCoordinates.Nullspace);
            var entityComponent = entity.GetComponent<HotReloadTestComponentOne>();

            Assert.That(entityComponent.Value, Is.EqualTo(5));
            Assert.That(entityComponent.Prototype, Is.Null);

            Assert.False(entity.HasComponent<HotReloadTestComponentTwo>());

            var reloaded = false;
            _prototypes.PrototypesReloaded += _ => reloaded = true;

            _prototypes.ReloadPrototypes(new List<IPrototype>());

            Assert.True(reloaded);
            reloaded = false;

            Assert.That(entityComponent.Value, Is.EqualTo(5));
            Assert.False(entity.HasComponent<HotReloadTestComponentTwo>());


            // Load string one, loading the hot reload prototype with a value of "A"
            AssertReload(ReloadedPrototypeStringOne);

            Assert.That(entityComponent.Value, Is.EqualTo(5));
            Assert.That(entityComponent.Prototype, Is.Null);

            // Load string two, changing the entity prototype and hot reload prototype
            AssertReload(ReloadedEntityStringTwo);

            // Existing component values are not modified in the current implementation
            Assert.That(entityComponent.Value, Is.EqualTo(5));
            Assert.That(entityComponent.Prototype, Is.Not.Null);
            Assert.That(entityComponent.Prototype!.Value, Is.EqualTo("A"));

            // New components are added
            Assert.True(entity.HasComponent<HotReloadTestComponentTwo>());


            // Load string three, changing the hot reload prototype to have a value of "B"
            AssertReload(ReloadedPrototypeStringThree);

            // Existing component values are not modified in the current implementation
            Assert.That(entityComponent.Value, Is.EqualTo(5));
            Assert.That(entityComponent.Prototype, Is.Not.Null);
            Assert.That(entityComponent.Prototype!.Value, Is.EqualTo("B"));

            // New components are added
            Assert.True(entity.HasComponent<HotReloadTestComponentTwo>());


            // Load the initial string again, changing the entity prototype but not removing the hot reload prototype
            AssertReload(InitialEntityString);

            // Existing component values are not modified in the current implementation
            Assert.That(entityComponent.Value, Is.EqualTo(5));

            // Old components are removed
            Assert.False(entity.HasComponent<HotReloadTestComponentTwo>());

            // Remove the prototype from the manager and check that it was removed from the component
            _prototypes.Clear();
            Assert.That(entityComponent.Prototype, Is.Null);
        }
    }

    public class HotReloadTestComponentOne : Component
    {
        public override string Name => HotReloadTest.HotReloadTestComponentOneId;

        [DataField("value")]
        public int Value { get; }

        [DataField("prototype")]
        public HotReloadPrototype? Prototype { get; }
    }

    public class HotReloadTestComponentTwo : Component
    {
        public override string Name => HotReloadTest.HotReloadTestComponentTwoId;
    }

    [Prototype("hotReload")]
    public class HotReloadPrototype : IPrototype
    {
        [DataField("id", required: true)]
        public string ID { get; } = string.Empty;

        [DataField("value")]
        public string Value { get; } = string.Empty;
    }
}
