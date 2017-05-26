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

        }

        [Test]
        public void TestBasicPrototype()
        {
            var prototype = manager.Index<EntityPrototype>("wrench");
            Assert.That(prototype.Name, Is.EqualTo("Not a wrench. Tricked!"));

            Dictionary<string, YamlNode> node = prototype.Components["TestBasicPrototypeComponent"];
            Assert.That(node["foo"], Is.EqualTo(new YamlScalarNode("bar!")));
        }

        [Test]
        public void TestLightPrototype()
        {
            var prototype = manager.Index<EntityPrototype>("wallLight");

            Assert.That(prototype.Name, Is.EqualTo("Wall Light"));
            Assert.That(prototype.Components.Keys, Contains.Item("Transform"));
            Assert.That(prototype.Components.Keys, Contains.Item("Velocity"));
            Assert.That(prototype.Components.Keys, Contains.Item("Direction"));
            Assert.That(prototype.Components.Keys, Contains.Item("Clickable"));
            Assert.That(prototype.Components.Keys, Contains.Item("Sprite"));
            Assert.That(prototype.Components.Keys, Contains.Item("BasicInteractable"));
            Assert.That(prototype.Components.Keys, Contains.Item("BasicMover"));
            Assert.That(prototype.Components.Keys, Contains.Item("WallMounted"));
            Assert.That(prototype.Components.Keys, Contains.Item("Light"));

            var componentData = prototype.Components["Light"];
            Assert.That(componentData, Contains.Item("startState").And.EqualTo("Off"));
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
        ";
    }

    [IoCTarget]
    [Component("TestBasicPrototypeComponent")]
    public class TestBasicPrototypeComponent : Component
    {

    }
}

