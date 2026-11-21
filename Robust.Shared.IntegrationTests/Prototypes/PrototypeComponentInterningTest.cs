using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.UnitTesting.Server;

namespace Robust.Shared.IntegrationTests.Prototypes;

[TestFixture]
internal sealed class PrototypeComponentInterningTest
{
    private const string ComponentName = "PrototypeInterned";
    private const string ParentId = "PrototypeComponentParent";
    private const string ChildAId = "PrototypeComponentChildA";
    private const string ChildBId = "PrototypeComponentChildB";
    private const string OverrideId = "PrototypeComponentOverride";
    private const string EqualAId = "PrototypeComponentEqualA";
    private const string EqualBId = "PrototypeComponentEqualB";
    private const string NullId = "PrototypeComponentNull";
    private const string StringNullId = "PrototypeComponentStringNull";

    [Test]
    public void InternsInheritedAndEquivalentComponents()
    {
        var sim = RobustServerSimulation
            .NewSimulation()
            .RegisterComponents(factory =>
            {
                factory.RegisterClass<PrototypeInternedComponent>();
                factory.RegisterClass<PrototypeNullableStringComponent>();
            })
            .RegisterPrototypes(factory => factory.LoadString(InitialPrototypes))
            .InitializeInstance();

        var componentFactory = sim.Resolve<IComponentFactory>();
        var prototypes = sim.Resolve<IPrototypeManager>();
        var childA = prototypes.Index<EntityPrototype>(ChildAId);
        var childB = prototypes.Index<EntityPrototype>(ChildBId);
        var overridden = prototypes.Index<EntityPrototype>(OverrideId);
        var equalA = prototypes.Index<EntityPrototype>(EqualAId);
        var equalB = prototypes.Index<EntityPrototype>(EqualBId);
        var nullPrototype = prototypes.Index<EntityPrototype>(NullId);
        var stringNullPrototype = prototypes.Index<EntityPrototype>(StringNullId);

        var childAEntry = childA.Components[ComponentName];
        Assert.Multiple(() =>
        {
            Assert.That(childAEntry.Component, Is.SameAs(childB.Components[ComponentName].Component), "Unchanged inherited components should share their prototype component.");
            Assert.That(childAEntry.Component, Is.Not.SameAs(overridden.Components[ComponentName].Component), "An overridden component must retain a distinct prototype component.");
            Assert.That(equalA.Components[ComponentName].Component, Is.SameAs(equalB.Components[ComponentName].Component), "Equivalent component mappings should share their prototype component.");
            Assert.That(childA.Components["Transform"].Component, Is.SameAs(childB.Components["Transform"].Component));
            Assert.That(childA.Components["MetaData"].Component, Is.SameAs(childB.Components["MetaData"].Component));
            Assert.That(nullPrototype.Components["PrototypeNullableString"].Component, Is.Not.SameAs(stringNullPrototype.Components["PrototypeNullableString"].Component));
            Assert.That(((PrototypeNullableStringComponent) nullPrototype.Components["PrototypeNullableString"].Component).Value, Is.Null);
            Assert.That(((PrototypeNullableStringComponent) stringNullPrototype.Components["PrototypeNullableString"].Component).Value, Is.EqualTo("null"));
        });

        var copied = (PrototypeInternedComponent) componentFactory.GetComponent(childAEntry);
        copied.Value = 100;

        var copiedTransform = componentFactory.GetComponent(childA.Components["Transform"]);
        var copiedMetaData = componentFactory.GetComponent(childA.Components["MetaData"]);
        copiedTransform.NetSyncEnabled = false;
        copiedMetaData.NetSyncEnabled = false;

        Assert.Multiple(() =>
        {
            Assert.That(copiedTransform, Is.Not.SameAs(childA.Components["Transform"].Component));
            Assert.That(copiedMetaData, Is.Not.SameAs(childA.Components["MetaData"].Component));
            Assert.That(childA.Components["Transform"].Component.NetSyncEnabled, Is.True);
            Assert.That(childA.Components["MetaData"].Component.NetSyncEnabled, Is.True);
        });

        Assert.Multiple(() =>
        {
            Assert.That(((PrototypeInternedComponent) childAEntry.Component).Value, Is.EqualTo(42));
            Assert.That(((PrototypeInternedComponent) childB.Components[ComponentName].Component).Value, Is.EqualTo(42));
        });

        var changed = new Dictionary<Type, HashSet<string>>();
        prototypes.LoadString(ReloadedParent, true, changed);
        prototypes.ReloadPrototypes(changed);

        childA = prototypes.Index<EntityPrototype>(ChildAId);
        childB = prototypes.Index<EntityPrototype>(ChildBId);

        // Assumption MAY change at some point
        Assert.Multiple(() =>
        {
            Assert.That(childA.Components[ComponentName].Component, Is.SameAs(childB.Components[ComponentName].Component), "The cache should be rebuilt after reload.");
            Assert.That(((PrototypeInternedComponent) childA.Components[ComponentName].Component).Value, Is.EqualTo(99));
        });
    }

    private static readonly string InitialPrototypes = $@"
- type: entity
  id: {ParentId}
  abstract: true
  components:
  - type: Transform
  - type: MetaData
  - type: {ComponentName}
    value: 42

- type: entity
  id: {ChildAId}
  parent: {ParentId}

- type: entity
  id: {ChildBId}
  parent: {ParentId}

- type: entity
  id: {OverrideId}
  parent: {ParentId}
  components:
  - type: {ComponentName}
    value: 77

- type: entity
  id: {EqualAId}
  components:
  - type: {ComponentName}
    value: 12

- type: entity
  id: {EqualBId}
  components:
  - type: {ComponentName}
    value: 12

- type: entity
  id: {NullId}
  components:
  - type: PrototypeNullableString
    value: null

- type: entity
  id: {StringNullId}
  components:
  - type: PrototypeNullableString
    value: ""null""
";

    private static readonly string ReloadedParent = $@"
- type: entity
  id: {ParentId}
  abstract: true
  components:
  - type: {ComponentName}
    value: 99
";
}

internal sealed partial class PrototypeInternedComponent : Component
{
    [DataField("value")]
    public int Value;
}

internal sealed partial class PrototypeNullableStringComponent : Component
{
    [DataField("value")]
    public string? Value;
}
