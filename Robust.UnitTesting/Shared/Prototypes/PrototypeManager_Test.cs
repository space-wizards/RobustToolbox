using System.IO;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
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
            Assert.That(mapping.GetValue<string>("foo"), Is.EqualTo("bar!"));
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

            Assert.That(componentData.GetValue<bool>("netsync"), Is.EqualTo(false));
        }

        [Test]
        public void TestYamlHelpersPrototype()
        {
            var prototype = manager.Index<EntityPrototype>("yamltester");
            Assert.That(prototype.Components, Contains.Key("TestBasicPrototypeComponent"));

            var componentData = prototype.Components["TestBasicPrototypeComponent"];

            Assert.That(componentData.GetValue<string>("str"), Is.EqualTo("hi!"));
            Assert.That(componentData.GetValue<int>("int"), Is.EqualTo(10));
            Assert.That(componentData.GetValue<float>("float"), Is.EqualTo(10f));
            Assert.That(componentData.GetValue<float>("float2"), Is.EqualTo(10.5f));
            Assert.That(componentData.GetValue<bool>("boolt"), Is.EqualTo(true));
            Assert.That(componentData.GetValue<bool>("boolf"), Is.EqualTo(false));
            Assert.That(componentData.GetValue<Vector2>("vec2"), Is.EqualTo(new Vector2(1.5f, 1.5f)));
            //todo Assert.That(componentData["vec2i"], Is.EqualTo(new Vector2i(1, 1)));
            Assert.That(componentData.GetValue<Vector3>("vec3"), Is.EqualTo(new Vector3(1.5f, 1.5f, 1.5f)));
            Assert.That(componentData.GetValue<Vector4>("vec4"), Is.EqualTo(new Vector4(1.5f, 1.5f, 1.5f, 1.5f)));
            Assert.That(componentData.GetValue<Color>("color"), Is.EqualTo(new Color(0xAA, 0xBB, 0xCC, 0xFF)));
            Assert.That(componentData.GetValue<YamlTestEnum>("enumf"), Is.EqualTo(YamlTestEnum.Foo));
            Assert.That(componentData.GetValue<YamlTestEnum>("enumb"), Is.EqualTo(YamlTestEnum.Bar));
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

        public enum YamlTestEnum : byte
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
    netsync: False

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
#    vec2i: 1, 1
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
    }

    public class TestBasicPrototypeComponent : Component
    {
        public override string Name => "TestBasicPrototypeComponent";

        [YamlField("foo")] public string Foo = null!;

        [YamlField("str")] public string Str = null!;

        [YamlField("anint")] public int? @int = null!;

        [YamlField("anfloat")] public float? @float = null!;

        [YamlField("float2")] public float? @float2 = null!;

        [YamlField("boolt")] public bool? @boolt = null!;

        [YamlField("boolf")] public bool? @boolf = null!;

        [YamlField("vec2")] public Vector2 vec2 = default;

        //todo [YamlField("vec2i")] public Vector2i vec2i = default;

        [YamlField("vec3")] public Vector3 vec3 = default;

        [YamlField("vec4")] public Vector4 vec4 = default;

        [YamlField("color")] public Color color = default;

        [YamlField("enumf")] public PrototypeManager_Test.YamlTestEnum enumf = default;

        [YamlField("enumb")] public PrototypeManager_Test.YamlTestEnum enumb = default;
    }
}
