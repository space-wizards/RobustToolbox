using JetBrains.Annotations;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.UnitTesting.Shared.Prototypes;

/// <summary>
/// Tests the <see cref="EntityPrototype"/> HasComp family of methods as well as <see cref="CompName"/> serialization.
/// </summary>
[UsedImplicitly]
[TestOf(typeof(EntityPrototype))]
[Description($"Tests the {nameof(EntityPrototype)} HasComp family of methods")]
internal sealed class PrototypeHasCompTest : OurRobustUnitTest
{
    private IComponentFactory _factory = default!;
    private IPrototypeManager _proto = default!;

    protected override Type[] ExtraComponents =>
    [
        typeof(TestDefinedComponent),
        typeof(TestInheritedComponent),
        typeof(TestMissingComponent),
        typeof(TestCompNameComponent)
    ];

    // TestEntity is expected to have TestDefinedComponent, TestInheritedComponent but not TestMissingComponent
    const string TestEntity = "TestEntity";
    // expected to have TestCompNameComponent
    const string TestInception = "TestInception";

    [OneTimeSetUp]
    public void Setup()
    {
        IoCManager.Resolve<ISerializationManager>().Initialize();
        _factory = IoCManager.Resolve<IComponentFactory>();
        _proto = IoCManager.Resolve<IPrototypeManager>();

        _proto.Initialize();
        _proto.LoadString(TestPrototypes);
        _proto.ResolveResults();
    }

    [Test]
    public void TestHasCompGeneric()
    {
        var proto = _proto.Index<EntityPrototype>(TestEntity);
        Assert.That(proto.HasComp<TestDefinedComponent>(_factory));
        Assert.That(proto.HasComp<TestInheritedComponent>(_factory));
        Assert.That(!proto.HasComp<TestMissingComponent>(_factory));

        // and with CompName separately
        Assert.That(proto.HasComp(_factory.CompName<TestDefinedComponent>()));
        Assert.That(proto.HasComp(_factory.CompName<TestInheritedComponent>()));
        Assert.That(!proto.HasComp(_factory.CompName<TestMissingComponent>()));
    }

    [Test]
    public void TestHasCompType()
    {
        var proto = _proto.Index<EntityPrototype>(TestEntity);

        // first test with GetRegistration
        var defined = _factory.GetRegistration<TestDefinedComponent>().Type;
        var inherited = _factory.GetRegistration<TestInheritedComponent>().Type;
        var missing = _factory.GetRegistration<TestMissingComponent>().Type;

        void TestThem()
        {
            Assert.That(proto.HasComp(defined, _factory));
            Assert.That(proto.HasComp(inherited, _factory));
            Assert.That(!proto.HasComp(missing, _factory));
        }

        TestThem();

        // then test with typeof
        defined = typeof(TestDefinedComponent);
        inherited = typeof(TestInheritedComponent);
        missing = typeof(TestMissingComponent);
        TestThem();
    }

    [Test]
    public void TestHasCompString()
    {
        var proto = _proto.Index<EntityPrototype>(TestEntity);
        var defined = new CompName("TestDefined", _factory);
        var inherited = new CompName("TestInherited", _factory);
        var missing = new CompName("TestMissing", _factory);
        Assert.That(proto.HasComp(defined));
        Assert.That(proto.HasComp(inherited));
        Assert.That(!proto.HasComp(missing));
    }

    [Test]
    public void TestCompNameSerialization()
    {
        var proto = _proto.Index<EntityPrototype>(TestInception);
        Assert.That(proto.TryComp<TestCompNameComponent>(out var inception, _factory));
        var name = inception!.Comp;
        Assert.That(name, Is.EqualTo(_factory.CompName<TestCompNameComponent>())); // inception
        Assert.That(proto.HasComp(name));
        Assert.That(_factory.HasRegistration(name));

        // gibberish component should prevent it from loading
        Assert.That(!_proto.HasIndex<EntityPrototype>("TestFail"));
    }

    const string TestPrototypes = $@"
- type: entity
  parent: TestEntityParent # making sure inheritance wouldn't break it
  id: {TestEntity}
  components:
  - type: TestDefined

- type: entity
  abstract: true
  id: TestEntityParent
  components:
  - type: TestInherited

- type: entity
  id: TestInception
  components:
  - type: TestCompName
    comp: TestCompName # inception

- type: entity
  id: TestFail
  components:
  - type: TestCompName
    comp: Afnuaghdjngbjda # this prevents this prototype from loading, hopefully nobody adds this component to rt :godo:
";
}

internal sealed partial class TestDefinedComponent : Component;
internal sealed partial class TestInheritedComponent : Component;
internal sealed partial class TestMissingComponent : Component;

internal sealed partial class TestCompNameComponent : Component
{
    [DataField(required: true)]
    public CompName Comp;
}
