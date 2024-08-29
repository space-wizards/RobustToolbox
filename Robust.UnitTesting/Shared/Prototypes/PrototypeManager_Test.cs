using System;
using System.Numerics;
using JetBrains.Annotations;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Vector3 = Robust.Shared.Maths.Vector3;
using Vector4 = Robust.Shared.Maths.Vector4;

namespace Robust.UnitTesting.Shared.Prototypes
{
    [UsedImplicitly]
    [TestFixture]
    public sealed class PrototypeManager_Test : RobustUnitTest
    {
        private const string LoadStringTestDummyId = "LoadStringTestDummy";
        private IPrototypeManager manager = default!;

        protected override Type[] ExtraComponents => new[] {typeof(TestBasicPrototypeComponent), typeof(PointLightComponent)};

        [OneTimeSetUp]
        public void Setup()
        {
            IoCManager.Resolve<ISerializationManager>().Initialize();
            manager = IoCManager.Resolve<IPrototypeManager>();
            manager.RegisterKind(typeof(EntityPrototype), typeof(EntityCategoryPrototype));
            manager.LoadString(DOCUMENT);
            manager.ResolveResults();
        }

        [Test]
        public void TestBasicPrototype()
        {
            var prototype = manager.Index<EntityPrototype>("wrench");
            Assert.That(prototype.Name, Is.EqualTo("Not a wrench. Tricked!"));

            var mapping = prototype.Components["TestBasicPrototype"].Component as TestBasicPrototypeComponent;
            Assert.That(mapping!.Foo, Is.EqualTo("bar!"));
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
                Assert.That(prototype.Components, Contains.Key("PointLight"));
            });

            var componentData = prototype.Components["PointLight"].Component as SharedPointLightComponent;

            Assert.That(componentData!.NetSyncEnabled, Is.EqualTo(false));
        }

        [Test]
        public void TestYamlHelpersPrototype()
        {
            var prototype = manager.Index<EntityPrototype>("yamltester");
            Assert.That(prototype.Components, Contains.Key("TestBasicPrototype"));

            var componentData = prototype.Components["TestBasicPrototype"].Component as TestBasicPrototypeComponent;

            Assert.That(componentData, Is.Not.Null);
            Assert.That(componentData!.Str, Is.EqualTo("hi!"));
            Assert.That(componentData!.int_field, Is.EqualTo(10));
            Assert.That(componentData!.float_field, Is.EqualTo(10f));
            Assert.That(componentData!.float2_field, Is.EqualTo(10.5f));
            Assert.That(componentData!.boolt, Is.EqualTo(true));
            Assert.That(componentData!.boolf, Is.EqualTo(false));
            Assert.That(componentData!.vec2, Is.EqualTo(new Vector2(1.5f, 1.5f)));
            Assert.That(componentData!.vec2i, Is.EqualTo(new Vector2i(1, 1)));
            Assert.That(componentData!.vec3, Is.EqualTo(new Vector3(1.5f, 1.5f, 1.5f)));
            Assert.That(componentData!.vec4, Is.EqualTo(new Vector4(1.5f, 1.5f, 1.5f, 1.5f)));
            Assert.That(componentData!.color, Is.EqualTo(new Color(0xAA, 0xBB, 0xCC, 0xFF)));
            Assert.That(componentData!.enumf, Is.EqualTo(YamlTestEnum.Foo));
            Assert.That(componentData!.enumb, Is.EqualTo(YamlTestEnum.Bar));
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
            manager.ResolveResults();

            var prototype = manager.Index<EntityPrototype>(LoadStringTestDummyId);

            Assert.That(prototype.Name, Is.EqualTo(LoadStringTestDummyId));
        }

        [Test]
        public void TestCircleException()
        {
            string GenerateCircleTestPrototype(string id, string parent)
            {
                return $@"- type: circle
  id: {id}
  parent: {parent}";
            }

            manager.RegisterKind(typeof(CircleTestPrototype));

            var directCircle = $@"{GenerateCircleTestPrototype("1", "2")}
{GenerateCircleTestPrototype("2", "1")}";

            Assert.Throws<PrototypeLoadException>(() => manager.LoadString(directCircle));
            manager.RemoveString(directCircle);

            var indirectCircle = $@"{GenerateCircleTestPrototype("1", "2")}
{GenerateCircleTestPrototype("2", "3")}
{GenerateCircleTestPrototype("3", "1")}";

            Assert.Throws<PrototypeLoadException>(() => manager.LoadString(indirectCircle));
        }

        [Prototype("circle")]
        private sealed partial class CircleTestPrototype : IPrototype, IInheritingPrototype
        {
            [IdDataField()]
            public string ID { get; private set; } = default!;
            [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<CircleTestPrototype>))]
            public string[]? Parents { get; private set; }
            [AbstractDataField]
            public bool Abstract { get; }
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
  - type: TestBasicPrototype
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
  - type: TestBasicPrototype
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

    public sealed partial class TestBasicPrototypeComponent : Component
    {

        [DataField("foo")] public string Foo = null!;

        [DataField("str")] public string Str = null!;

        [DataField("int")] public int? int_field = null!;

        [DataField("float")] public float? float_field = null!;

        [DataField("float2")] public float? float2_field = null!;

        [DataField("boolt")] public bool? @boolt = null!;

        [DataField("boolf")] public bool? @boolf = null!;

        [DataField("vec2")] public Vector2 vec2 = default;

        [DataField("vec2i")] public Vector2i vec2i = default;

        [DataField("vec3")] public Vector3 vec3 = default;

        [DataField("vec4")] public Vector4 vec4 = default;

        [DataField("color")] public Color color = default;

        [DataField("enumf")] public PrototypeManager_Test.YamlTestEnum enumf = default;

        [DataField("enumb")] public PrototypeManager_Test.YamlTestEnum enumb = default;
    }
}
