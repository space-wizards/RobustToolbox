using JetBrains.Annotations;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Robust.UnitTesting.Shared.Prototypes;

/// <summary>
/// Tests the <see cref="EntityPrototype"/> HasComp family of methods.
/// </summary>
[UsedImplicitly]
[TestFixture]
internal sealed class PrototypeHasCompTest : OurRobustUnitTest
{
    private IComponentFactory _factory = default!;
    private IPrototypeManager _proto = default!;

    protected override Type[] ExtraComponents =>
    [
        typeof(TestDefinedComponent),
        typeof(TestInheritedComponent),
        typeof(TestMissingComponent)
    ];

    // TestEntity is expected to have TestDefinedComponent, TestInheritedComponent but not TestMissingComponent
    const string TestEntity = "TestEntity";

    [OneTimeSetUp]
    public void Setup()
    {
        IoCManager.Resolve<ISerializationManager>().Initialize();
        _factory = IoCManager.Resolve<IComponentFactory>();
        _proto = IoCManager.Resolve<IPrototypeManager>();

        _proto.RegisterKind(typeof(EntityPrototype), typeof(EntityCategoryPrototype));
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
        // intentionally skirting ForbidLiteral here
        var defined = "TestDefined";
        var inherited = "TestInherited";
        var missing = "TestMissing";
        Assert.That(proto.HasComp(defined));
        Assert.That(proto.HasComp(inherited));
        Assert.That(!proto.HasComp(missing));
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
";
}

internal sealed partial class TestDefinedComponent : Component;
internal sealed partial class TestInheritedComponent : Component;
internal sealed partial class TestMissingComponent : Component;
