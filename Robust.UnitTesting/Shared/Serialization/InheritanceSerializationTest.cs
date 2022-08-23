using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Definition;

namespace Robust.UnitTesting.Shared.Serialization
{
    [TestFixture]
    [TestOf(typeof(DataDefinition))]
    public sealed class InheritanceSerializationTest : RobustUnitTest
    {
        private const string BaseEntityId = "BaseEntity";
        private const string InheritorEntityId = "InheritorEntityId";
        private const string FinalEntityId = "FinalEntityId";

        private const string BaseComponentFieldValue = "BaseFieldValue";
        private const string InheritorComponentFieldValue = "InheritorFieldValue";
        private const string FinalComponentFieldValue = "FinalFieldValue";

        private static readonly string Prototypes = $@"
- type: entity
  id: {BaseEntityId}
  components:
  - type: TestBase
    baseField: {BaseComponentFieldValue}

- type: entity
  id: {InheritorEntityId}
  components:
  - type: TestInheritor
    baseField: {BaseComponentFieldValue}
    inheritorField: {InheritorComponentFieldValue}

- type: entity
  id: {FinalEntityId}
  components:
  - type: TestFinal
    baseField: {BaseComponentFieldValue}
    inheritorField: {InheritorComponentFieldValue}
    finalField: {FinalComponentFieldValue}";

        [Test]
        public void Test()
        {
            var componentFactory = IoCManager.Resolve<IComponentFactory>();

            componentFactory.RegisterClass<TestBaseComponent>();
            componentFactory.RegisterClass<TestInheritorComponent>();
            componentFactory.RegisterClass<TestFinalComponent>();
            componentFactory.GenerateNetIds();

            var serializationManager = IoCManager.Resolve<ISerializationManager>();
            serializationManager.Initialize();

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            prototypeManager.RegisterType(typeof(EntityPrototype));
            prototypeManager.LoadString(Prototypes);
            prototypeManager.ResolveResults();

            var entityManager = IoCManager.Resolve<IEntityManager>();

            var mapManager = IoCManager.Resolve<IMapManager>();

            var mapId = new MapId(1);

            mapManager.CreateMap(mapId);

            var coordinates = new MapCoordinates(0, 0, mapId);

            var baseEntity = entityManager.SpawnEntity(BaseEntityId, coordinates);

            Assert.That(IoCManager.Resolve<IEntityManager>().TryGetComponent(baseEntity, out TestBaseComponent? baseComponent));
            Assert.That(baseComponent!.BaseField, Is.EqualTo(BaseComponentFieldValue));

            var inheritorEntity = entityManager.SpawnEntity(InheritorEntityId, coordinates);

            Assert.That(IoCManager.Resolve<IEntityManager>().TryGetComponent(inheritorEntity, out TestInheritorComponent? inheritorComponent));
            Assert.That(inheritorComponent!.BaseField, Is.EqualTo(BaseComponentFieldValue));
            Assert.That(inheritorComponent!.InheritorField, Is.EqualTo(InheritorComponentFieldValue));

            var finalEntity = entityManager.SpawnEntity(FinalEntityId, coordinates);

            Assert.That(IoCManager.Resolve<IEntityManager>().TryGetComponent(finalEntity, out TestFinalComponent? finalComponent));
            Assert.That(finalComponent!.BaseField, Is.EqualTo(BaseComponentFieldValue));
            Assert.That(finalComponent!.InheritorField, Is.EqualTo(InheritorComponentFieldValue));
            Assert.That(finalComponent!.FinalField, Is.EqualTo(FinalComponentFieldValue));
        }
    }

    [Virtual]
    public class TestBaseComponent : Component
    {

        [DataField("baseField")] public string? BaseField;
    }

    [Virtual]
    public class TestInheritorComponent : TestBaseComponent
    {

        [DataField("inheritorField")] public string? InheritorField;
    }

    public sealed class TestFinalComponent : TestInheritorComponent
    {

        [DataField("finalField")] public string? FinalField;
    }
}
