using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;
using Robust.UnitTesting.Shared;

namespace Robust.Shared.IntegrationTests.Serialization.TypeSerializers.Custom;

/// <summary>
/// Tests to ensure that <see cref="OrganizedListSerializer{T}"/> properly organizes a very simple list
/// </summary>
[TestFixture]
[TestOf(typeof(OrganizedListSerializer<>))]
internal sealed partial class OrganizedListSerializerTest : OurRobustUnitTest
{
    private const string ParentProto = "ProtoA";

    private const string ChildProto = "ProtoB";

    private List<int> _key0Values = [2, 4, 8];

    private List<int> _key1Values = [1, 2, 2, 3, 4, 8];

    private List<int> _key2Values = [1, 3, 9];

    private List<int> _key1ValuesB = [1, 2, 3];

    private List<int> _key2ValuesB = [2, 4, 8];

    protected override Type[] ExtraComponents => new[] {typeof(TestComponent)};

    private static readonly string Prototypes = $@"
- type: entity
  id: {ParentProto}
  components:
  - type: Test
    listA:
    - key: 1
      values:
      - 2
      - 4
      - 8
    - key: 2
      values:
      - 1
      - 3
      - 9
    listB:
    - key: 1
      values:
      - 2
      - 4
      - 8
    - key: 2
      values:
      - 1
      - 3
      - 9

- type: entity
  parent: [{ParentProto}]
  id: {ChildProto}
  components:
  - type: Test
    listA:
    - key: 1
      values:
      - 1
      - 2
      - 3
    - key: 0
      values:
      - 2
      - 4
      - 8
    listB:
    - key: 1
      values:
      - 1
      - 2
      - 3
    - key: 2
      values:
      - 2
      - 4
      - 8
";

    [Test]
    public void OrganizedListTest()
    {
        var serializationManager = IoCManager.Resolve<ISerializationManager>();
        serializationManager.Initialize();
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

        // We use entity prototypes since Serialization recognizes them without fuss, and we need complex YAML behavior for this test.
        prototypeManager.RegisterKind(typeof(EntityPrototype), typeof(EntityCategoryPrototype));
        prototypeManager.LoadString(Prototypes);
        prototypeManager.ResolveResults();

        var entityManager = IoCManager.Resolve<IEntityManager>();
        entityManager.System<SharedMapSystem>().CreateMap(out var mapId);

        var coordinates = new MapCoordinates(0, 0, mapId);

        var parentEntity = entityManager.SpawnEntity(ParentProto, coordinates);
        var childEntity = entityManager.SpawnEntity(ChildProto, coordinates);

        Assert.That(entityManager.TryGetComponent<TestComponent>(parentEntity, out var parentComp));
        Assert.That(entityManager.TryGetComponent<TestComponent>(childEntity, out var childComponent));

        // Instead of 4 items, two of them should've combined into one.
        Assert.That(childComponent!.ListA.Count == 3, $"Child Component's AList failed to inherit properly. List contained {childComponent!.ListA.Count} entries.");

        var index = 0;
        // Ensure that the lists inherited correctly, and became organized and squashed correctly
        foreach (var obj in childComponent.ListA)
        {
            // Items should be ordered!
            Assert.That(obj.Key == index);
            switch (obj.Key)
            {
                case 0:
                    // Should be an unmodified list.
                    Assert.That(obj.Values.SequenceEqual(_key0Values));
                    break;
                case 1:
                    for (var i = obj.Values.Count - 1; i >= 0; i--)
                    {
                        var item = obj.Values[i];
                        Assert.That(obj.Values.Count == _key1Values.Count);
                        for (var j = _key1Values.Count - 1; j >= 0; j--)
                        {
                            if (_key1Values[j] != item)
                                continue;

                            _key1Values.RemoveSwap(j);
                            obj.Values.RemoveSwap(i);
                            break;
                        }
                    }
                    Assert.That(obj.Values.Count == 0 && _key1Values.Count == 0);
                    break;
                case 2:
                    Assert.That(obj.Values.SequenceEqual(_key2Values));
                    break;
                default:
                    Assert.Fail($"Key was an unexpected value {obj.Key}");
                    break;
            }

            index++;
        }

        // We alternate between keys 1 and 2 :P
        index = 1;
        // Ensure that this list followed normal push inheritance rules
        for (var i = 0; i < childComponent.ListB.Count; i++)
        {
            // The greatest code known to man :)
            var childList = childComponent.ListB[i];
            switch (i)
            {
                case 0:
                    Assert.That(childList.Values.Count == _key1ValuesB.Count);
                    for (var j = 0; j < childList.Values.Count; j++)
                    {
                        Assert.That(childList.Values[j] == _key1ValuesB[j]);
                    }
                    index++;
                    break;
                case 1:
                    Assert.That(childList.Values.Count == _key2ValuesB.Count);
                    for (var j = 0; j < childList.Values.Count; j++)
                    {
                        Assert.That(childList.Values[j] == _key2ValuesB[j]);
                    }
                    index--;
                    break;
                case 2:
                    var parentList = parentComp!.ListB[i - 2];
                    Assert.That(childList.Values.Count == parentList.Values.Count);
                    for (var j = 0; j < childList.Values.Count; j++)
                    {
                        Assert.That(childList.Values[j] == parentList.Values[j]);
                    }
                    index++;
                    break;
                case 3:
                    parentList = parentComp!.ListB[i - 2];
                    Assert.That(childList.Values.Count == parentList.Values.Count);
                    for (var j = 0; j < childList.Values.Count; j++)
                    {
                        Assert.That(childList.Values[j] == parentList.Values[j]);
                    }
                    index--;
                    break;
                default:
                    Assert.Fail($"ListB contained more elements than expected: {childComponent.ListB.Count}");
                    break;
            }
        }
    }
}

[DataDefinition]
public sealed partial class ComplexTestObject : IOrganizeableCollection<ComplexTestObject>
{
    [DataField(required: true)]
    public int Key;

    [DataField(required: true)]
    public List<int> Values = new ();

    public int CompareTo(ComplexTestObject? other)
    {
        return Key.CompareTo(other?.Key);
    }

    public bool Equals(ComplexTestObject? other)
    {
        return Key.Equals(other?.Key);
    }

    public void Insert(ComplexTestObject other)
    {
        Values.AddRange(other.Values) ;
    }
}

public sealed partial class TestComponent : Component
{
    [DataField(customTypeSerializer: typeof(OrganizedListSerializer<ComplexTestObject>))]
    [AlwaysPushInheritance]
    public List<ComplexTestObject> ListA = new ();

    [DataField]
    [AlwaysPushInheritance]
    public List<ComplexTestObject> ListB = new ();
}
