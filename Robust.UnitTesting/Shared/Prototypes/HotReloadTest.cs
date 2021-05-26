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
    public class HotReloadTest : RobustUnitTest
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
            _components.RegisterClass<HotReloadTestComponentOne>();
            _components.RegisterClass<HotReloadTestComponentTwo>();

            IoCManager.Resolve<ISerializationManager>().Initialize();
            _prototypes = (PrototypeManager) IoCManager.Resolve<IPrototypeManager>();
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
            var entityComponent = entity.GetComponent<HotReloadTestComponentOne>();

            Assert.That(entityComponent.Value, Is.EqualTo(5));
            Assert.False(entity.HasComponent<HotReloadTestComponentTwo>());

            _prototypes.ReloadPrototypes(new List<IPrototype>());

            Assert.That(entityComponent.Value, Is.EqualTo(5));
            Assert.False(entity.HasComponent<HotReloadTestComponentTwo>());

            var changedPrototypes = _prototypes.LoadString(ReloadedPrototypes, true);
            _prototypes.ReloadPrototypes(changedPrototypes);

            // Existing component values are not modified in the current implementation
            Assert.That(entityComponent.Value, Is.EqualTo(5));

            // New components are added
            Assert.True(entity.HasComponent<HotReloadTestComponentTwo>());

            changedPrototypes = _prototypes.LoadString(InitialPrototypes, true);
            _prototypes.ReloadPrototypes(changedPrototypes);

            // Existing component values are not modified in the current implementation
            Assert.That(entityComponent.Value, Is.EqualTo(5));

            // Old components are removed
            Assert.False(entity.HasComponent<HotReloadTestComponentTwo>());
        }
    }

    public class HotReloadTestComponentOne : Component
    {
        public override string Name => HotReloadTest.HotReloadTestComponentOneId;

        [DataField("value")]
        public int Value { get; }
    }

    public class HotReloadTestComponentTwo : Component
    {
        public override string Name => HotReloadTest.HotReloadTestComponentTwoId;
    }
}
