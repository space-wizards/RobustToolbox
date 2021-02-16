using System.IO;
using System.Linq;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Prototypes.DataClasses.Attributes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using YamlDotNet.RepresentationModel;

namespace Robust.UnitTesting.Shared.ComponentData
{
    [TestFixture]
    public class ComponentSerializationTest : RobustUnitTest
    {
        private string prototype = @"
- type: entity
  id: TestEntity
  components:
  - type: TestComp
    foo: 1
    baz: Testing

- type: entity
  id: CustomTestEntity
  components:
  - type: CustomTestComp
    abc: foo

- type: entity
  id: CustomInheritTestEntity
  components:
  - type: CustomTestCompInheritor
    abc: foo
";

        [OneTimeSetUp]
        public void Setup()
        {
            IoCManager.Resolve<IComponentFactory>().Register<SerializationTestComponent>();
            IoCManager.Resolve<IComponentFactory>().Register<TestCustomDataClassComponent>();
            IoCManager.Resolve<IComponentFactory>().Register<TestCustomDataClassInheritorComponent>();
            IoCManager.Resolve<IComponentManager>().Initialize();

            IoCManager.Resolve<IDataClassManager>().Initialize();
            IoCManager.Resolve<ISerializationManager>().Initialize();

            IoCManager.Resolve<IPrototypeManager>().LoadFromStream(new StringReader(prototype));
            IoCManager.Resolve<IPrototypeManager>().Resync();

        }

        [Test]
        public void ParsingTest()
        {
            var data = IoCManager.Resolve<IPrototypeManager>().Index<EntityPrototype>("TestEntity");

            /*todo paul
             Assert.That(data.Components["TestComp"] is TestCom)
            Assert.That(data.Components["TestComp"].GetValue("foo"), Is.EqualTo(1));
            Assert.That(data.Components["TestComp"].GetValue("bar"), Is.Null);
            Assert.That(data.Components["TestComp"].GetValue("baz"), Is.EqualTo("Testing"));*/
        }

        [Test]
        public void PopulatingTest()
        {
            var entity = IoCManager.Resolve<IEntityManager>().CreateEntityUninitialized("TestEntity");
            var comp = entity.GetComponent<SerializationTestComponent>();
            Assert.That(comp.Foo, Is.EqualTo(1));
            Assert.That(comp.Bar, Is.EqualTo(-1));
            Assert.That(comp.Baz, Is.EqualTo("Testing"));
        }

        [Test]
        public void SerializationTest()
        {
            var entity = IoCManager.Resolve<IEntityManager>().CreateEntityUninitialized("TestEntity");
            var comp = entity.GetComponent<SerializationTestComponent>();
            var dataclass = IoCManager.Resolve<IDataClassManager>().GetEmptyDataClass(comp.GetType())!;

            var mapping = new YamlMappingNode();
            IoCManager.Resolve<ISerializationManager>()
                .Serialize(dataclass.GetType(), comp, YamlObjectSerializer.NewWriter(mapping));
            Assert.That(mapping, Is.Not.Null);
            Assert.That(mapping!.Children.ContainsKey("foo"));
            Assert.That(mapping.Children["foo"], Is.EqualTo(new YamlScalarNode("1")));
            Assert.That(mapping!.Children.ContainsKey("baz"));
            Assert.That(mapping.Children["baz"], Is.EqualTo(new YamlScalarNode("Testing")));
            Assert.That(!mapping!.Children.ContainsKey("bar"));
        }

        [Test]
        public void CustomDataClassTest()
        {
            var entity = IoCManager.Resolve<IEntityManager>().CreateEntityUninitialized("CustomTestEntity");
            var comp = entity.GetComponent<TestCustomDataClassComponent>();
            Assert.That(comp.Abc, Is.EqualTo("foo"));
        }

        [Test]
        public void CustomDataClassInheritanceTest()
        {
            var entity = IoCManager.Resolve<IEntityManager>().CreateEntityUninitialized("CustomInheritTestEntity");
            var comp = entity.GetComponent<TestCustomDataClassInheritorComponent>();
            Assert.That(comp.Abc, Is.EqualTo("foo"));
        }

        private class SerializationTestComponent : Component
        {
            public override string Name => "TestComp";

            [YamlField("foo")]
            public int Foo = -1;

            [YamlField("bar")]
            public int Bar = -1;

            [YamlField("baz")]
            public string Baz = "abc";
        }

        [DataClass(typeof(ACustomDataClassWithARandomName))]
        private class TestCustomDataClassComponent : Component
        {
            public override string Name => "CustomTestComp";

            [DataClassTarget("abc")]
            public string Abc = "ERROR";
        }

        private class TestCustomDataClassInheritorComponent : TestCustomDataClassComponent
        {
            public override string Name => "CustomTestCompInheritor";
        }
    }

    public partial class ACustomDataClassWithARandomName
    {
        [DataClassTarget("abc")]
        public string? Abc;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref Abc, "abc", null);
        }
    }
}
