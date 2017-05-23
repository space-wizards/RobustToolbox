using NUnit.Framework;
using SS14.Shared.IoC;
using SS14.Shared.Prototypes;
using SS14.Shared.GameObjects;
using System.IO;
using YamlDotNet.RepresentationModel;

namespace SS14.UnitTesting.SS14.Shared.Prototypes
{
    [TestFixture]
    public class PrototypeManager_Test : SS14UnitTest
    {
        [Test]
        public void TestBasicPrototype()
        {
            var manager = IoCManager.Resolve<IPrototypeManager>();
            manager.LoadFromStream(new StringReader(DOCUMENT));
            var prototype = manager.Index<EntityPrototype>("wrench");
            Assert.That(prototype.Name, Is.EqualTo("Not a wrench. Tricked!"));
            YamlMappingNode node = prototype.Components[typeof(TestBasicPrototypeComponent)];
            Assert.That(node[new YamlScalarNode("foo")], Is.EqualTo(new YamlScalarNode("bar!")));
        }

        const string DOCUMENT = @"
- type: entity
  id: wrench
  name: Not a wrench. Tricked!
  components:
  - type: TestBasicPrototypeComponent
    foo: bar!
        ";
    }

    [IoCTarget]
    [Component("TestBasicPrototypeComponent")]
    public class TestBasicPrototypeComponent : Component
    {

    }
}

