using System.IO;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.UnitTesting.Shared.Prototypes
{
    [TestFixture]
    public class PrototypeManager_Test : RobustUnitTest
    {
        private const string LoadStringTestDummyId = "LoadStringTestDummy";
        private IPrototypeManager manager = default!;

        [OneTimeSetUp]
        public void Setup()
        {
            var factory = IoCManager.Resolve<IComponentFactory>();
            factory.Register<TestBasicPrototypeComponent>();

            manager = IoCManager.Resolve<IPrototypeManager>();
            manager.LoadFromStream(new StringReader(DOCUMENT));
            manager.Resync();
        }

        [Test]
        public void TestBasicPrototype()
        {
            var prototype = manager.Index<EntityPrototype>("wrench");
            Assert.That(prototype.Name, Is.EqualTo("Not a wrench. Tricked!"));

            var mapping = prototype.Components["TestBasicPrototypeComponent"];
            Assert.That(mapping.GetNode("foo"), Is.EqualTo(new YamlScalarNode("bar!")));
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
                Assert.That(prototype.Components, Contains.Key("Sprite"));
                Assert.That(prototype.Components, Contains.Key("PointLight"));
            });

            var componentData = prototype.Components["PointLight"];
            var expected = new YamlMappingNode();
            expected.Children[new YamlScalarNode("startState")] = new YamlScalarNode("Off");

            Assert.That(componentData, Is.EquivalentTo(expected));
        }

        [Test]
        public void TestYamlHelpersPrototype()
        {
            var prototype = manager.Index<EntityPrototype>("yamltester");
            Assert.That(prototype.Components, Contains.Key("TestBasicPrototypeComponent"));

            var componentData = prototype.Components["TestBasicPrototypeComponent"];

            Assert.That(componentData["str"].AsString(), Is.EqualTo("hi!"));
            Assert.That(componentData["int"].AsInt(), Is.EqualTo(10));
            Assert.That(componentData["float"].AsFloat(), Is.EqualTo(10f));
            Assert.That(componentData["float2"].AsFloat(), Is.EqualTo(10.5f));
            Assert.That(componentData["boolt"].AsBool(), Is.EqualTo(true));
            Assert.That(componentData["boolf"].AsBool(), Is.EqualTo(false));
            Assert.That(componentData["vec2"].AsVector2(), Is.EqualTo(new Vector2(1.5f, 1.5f)));
            Assert.That(componentData["vec2i"].AsVector2i(), Is.EqualTo(new Vector2i(1, 1)));
            Assert.That(componentData["vec3"].AsVector3(), Is.EqualTo(new Vector3(1.5f, 1.5f, 1.5f)));
            Assert.That(componentData["vec4"].AsVector4(), Is.EqualTo(new Vector4(1.5f, 1.5f, 1.5f, 1.5f)));
            Assert.That(componentData["color"].AsHexColor(), Is.EqualTo(new Color(0xAA, 0xBB, 0xCC, 0xFF)));
            Assert.That(componentData["enumf"].AsEnum<YamlTestEnum>(), Is.EqualTo(YamlTestEnum.Foo));
            Assert.That(componentData["enumb"].AsEnum<YamlTestEnum>(), Is.EqualTo(YamlTestEnum.Bar));
        }

        [Test]
        public void TestPlacementProperties()
        {
            var prototype = manager.Index<EntityPrototype>("mounttester");

            Assert.That(prototype.MountingPoints, Is.EquivalentTo(new int[] { 1, 2, 3 }));
            Assert.That(prototype.PlacementMode, Is.EqualTo("AlignWall"));
            Assert.That(prototype.PlacementRange, Is.EqualTo(300));
            Assert.That(prototype.PlacementOffset, Is.EqualTo(new Vector2i(30, 45)));
        }

        [Test]
        public void TestPlacementInheritance()
        {
            var prototype = manager.Index<EntityPrototype>("PlaceInheritTester");

            Assert.That(prototype.PlacementMode, Is.EqualTo("SnapgridCenter"));
        }

        [Test]
        public void TestLoadString()
        {
            manager.LoadString(LoadStringDocument);

            var prototype = manager.Index<EntityPrototype>(LoadStringTestDummyId);

            Assert.That(prototype.Name, Is.EqualTo(LoadStringTestDummyId));
        }

        private enum YamlTestEnum : byte
        {
            Foo,
            Bar
        }

        const string DOCUMENT = @"
- type: entity
  id: wrench
  name: Not a wrench. Tricked!
  components:
  - type: TestBasicPrototypeComponent
    foo: bar!

- type: entity
  id: wallLight
  name: Wall Light
  components:
  - type: Transform
  - type: Sprite
  - type: PointLight
    startState: Off

- type: entity
  id: wallLightChild
  parent: wallLight

- type: entity
  id: yamltester
  components:
  - type: TestBasicPrototypeComponent
    str: hi!
    int: 10
    float: 10
    float2: 10.5
    boolt: true
    boolf: false
    vec2: 1.5, 1.5
    vec2i: 1, 1
    vec3: 1.5, 1.5, 1.5
    vec4: 1.5, 1.5, 1.5, 1.5
    color: '#aabbcc'
    enumf: Foo
    enumb: Bar

- type: entity
  id: mounttester
  placement:
    mode: AlignWall
    range: 300
    offset: 30, 45
    nodes:
    - 1
    - 2
    - 3

- type: entity
  id: PlaceInheritTester
  parent: mounttester
  placement:
    mode: SnapgridCenter
";

        private static readonly string LoadStringDocument = $@"
- type: entity
  id: {LoadStringTestDummyId}
  name: {LoadStringTestDummyId}";
    }

    public class TestBasicPrototypeComponent : Component
    {
        public override string Name => "TestBasicPrototypeComponent";
    }
}
