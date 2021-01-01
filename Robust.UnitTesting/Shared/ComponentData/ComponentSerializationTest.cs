using System.IO;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.UnitTesting.Server.Maps;

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
";

        [OneTimeSetUp]
        public void Setup()
        {
            IoCManager.Resolve<IComponentFactory>().Register<SerializationTestComponent>();
            IoCManager.Resolve<IComponentManager>().Initialize();

            IoCManager.Resolve<IPrototypeManager>().LoadFromStream(new StringReader(prototype));
            IoCManager.Resolve<IPrototypeManager>().Resync();
        }

        [Test]
        public void ParsingTest()
        {
            var data = IoCManager.Resolve<IPrototypeManager>().Index<EntityPrototype>("TestEntity");
            Assert.That(data.Components["TestComp"].GetValue("foo"), Is.EqualTo(1));
            Assert.That(data.Components["TestComp"].GetValue("bar"), Is.Null);
            Assert.That(data.Components["TestComp"].GetValue("baz"), Is.EqualTo("Testing"));
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
    }
}
