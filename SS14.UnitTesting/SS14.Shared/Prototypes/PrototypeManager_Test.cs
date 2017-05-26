using NUnit.Framework;
using SS14.Shared.IoC;
using SS14.Shared.Prototypes;
using SS14.Shared.GameObjects;
using System.IO;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace SS14.UnitTesting.SS14.Shared.Prototypes
{
    [TestFixture]
    public class PrototypeManager_Test : SS14UnitTest
    {
        private IPrototypeManager manager;
        protected override void Init()
        {
            manager = IoCManager.Resolve<IPrototypeManager>();
            manager.LoadFromStream(new StringReader(DOCUMENT));
            manager.Resync();

        }

        [Test]
        public void TestBasicPrototype()
        {
            var prototype = manager.Index<EntityPrototype>("wrench");
            Assert.That(prototype.Name, Is.EqualTo("Not a wrench. Tricked!"));

            Dictionary<string, YamlNode> node = prototype.Components["TestBasicPrototypeComponent"];
            Assert.That(node["foo"], Is.EqualTo(new YamlScalarNode("bar!")));
        }

        [Test, Combinatorial]
        public void TestLightPrototype([Values("wallLight", "wallLightChild")] string id)
        {
            var prototype = manager.Index<EntityPrototype>(id);

            Assert.Multiple(() =>
            {
                Assert.That(prototype.Name, Is.EqualTo("Wall Light"));
                Assert.That(prototype.ID, Is.EqualTo(id));
                Assert.That(prototype.Components, Contains.Key("Transform"));
                Assert.That(prototype.Components, Contains.Key("Velocity"));
                Assert.That(prototype.Components, Contains.Key("Direction"));
                Assert.That(prototype.Components, Contains.Key("Clickable"));
                Assert.That(prototype.Components, Contains.Key("Sprite"));
                Assert.That(prototype.Components, Contains.Key("BasicInteractable"));
                Assert.That(prototype.Components, Contains.Key("BasicMover"));
                Assert.That(prototype.Components, Contains.Key("WallMounted"));
                Assert.That(prototype.Components, Contains.Key("Light"));
            });

            var componentData = prototype.Components["Light"];
            var expected = new Dictionary<string, YamlNode>();
            expected["startState"] = new YamlScalarNode("Off");

            Assert.That(componentData, Is.EquivalentTo(expected));
        }

        const string DOCUMENT = @"
- type: entity
  id: wrench
  name: Not a wrench. Tricked!
  components:
  - type: TestBasicPrototypeComponent
    foo: bar!
---
- type: entity
  id: wallLight
  name: Wall Light
  components:
  - type: Transform
  - type: Velocity
  - type: Direction
  - type: Clickable
  - type: Sprite
  - type: BasicInteractable
  - type: BasicMover
  - type: WallMounted
  - type: Light
    startState: Off
---
- type: entity
  id: wallLightChild
  parent: wallLight
";
    }

    [IoCTarget]
    [Component("TestBasicPrototypeComponent")]
    public class TestBasicPrototypeComponent : Component
    {

    }
}

