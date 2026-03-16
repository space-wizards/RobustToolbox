using System.Numerics;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Robust.UnitTesting.Shared.GameObjects.EntityBuilder;

internal sealed class EntityBuilderTests : OurRobustUnitTest
{
    private const string TestEnt1 = "T_TestEnt1";

    private PrototypeManager _protoMan = default!;
    private IEntityManager _entMan = default!;

    private const string Prototypes = $"""
        - type: entity
          id: {TestEnt1}
          components:
          - type: MetaData # Test of new spawn logic..
          - type: Transform
            noRot: true
          - type: Marker1
          - type: Marker2
          - type: Marker3
          - type: Marker4
        """;

    protected override Type[] ExtraComponents =>
    [
        typeof(Marker1Component), typeof(Marker2Component), typeof(Marker3Component), typeof(Marker4Component)
    ];

    [OneTimeSetUp]
    public void Setup()
    {
        IoCManager.Resolve<ISerializationManager>().Initialize();

        _protoMan = (PrototypeManager) IoCManager.Resolve<IPrototypeManager>();
        _protoMan.RegisterKind(typeof(EntityPrototype), typeof(EntityCategoryPrototype));
        _protoMan.LoadString(Prototypes);
        _protoMan.ResolveResults();

        _entMan = IoCManager.Resolve<IEntityManager>();
    }

    [Test]
    [Description("Creates a few entities with blank EntityBuilders, ensuring component adds work.")]
    public void CreateEntity()
    {
        var builder = _entMan.EntityBuilder()
            .Named("Test entity")
            .AddComp<Marker1Component>()
            .AddComp<Marker2Component>();

        var ent = _entMan.ApplyEntityBuilder(builder);

        Assert.That(_entMan.HasComponent<Marker1Component>(ent), "Expected builder added components on the new entity");
        Assert.That(_entMan.HasComponent<Marker1Component>(ent), "Expected builder added components on the new entity");
    }

    [Test]
    [Description("Creates a hierarchy of entities, a map and some grids, using blank entity builders.")]
    public void CreateHierarchy()
    {
        var root = _entMan.EntityBuilder()
            .Named("Test entity")
            .AddComp<MapComponent>()
            .AddComp<Marker1Component>();

        var child1 = _entMan.EntityBuilder()
            .Named("Test child 1")
            .ChildOf(root.ReservedEntity, new Vector2(-4, -4))
            .AddComp<MapGridComponent>()
            .AddComp<Marker1Component>();

        var child2 = _entMan.EntityBuilder()
            .Named("Test child 2")
            .ChildOf(root.ReservedEntity, new Vector2(4, 4))
            .AddComp<MapGridComponent>()
            .AddComp<Marker2Component>();

        // Spawn the map and its children.
        // Note: Currently order does matter.
        _entMan.BulkApplyEntityBuilders([root, child1, child2]);

        Assert.That(_entMan.GetComponent<MapComponent>(root.ReservedEntity).MapId,
            NUnit.Framework.Is.Not.EqualTo(MapId.Nullspace));

        Assert.That(_entMan.GetComponent<TransformComponent>(root.ReservedEntity)._children,
            NUnit.Framework.Is.EquivalentTo([child1.ReservedEntity, child2.ReservedEntity]),
            "Expected the hierarchy we set up to be respected.");
    }

    [Test]
    public void CreateFromPrototype()
    {
        var builder = _entMan.EntityBuilder(TestEnt1);

        _entMan.ApplyEntityBuilder(builder);

        Assert.That(_entMan.HasComponent<MetaDataComponent>(builder.ReservedEntity));
        Assert.That(_entMan.HasComponent<TransformComponent>(builder.ReservedEntity));
        Assert.That(_entMan.HasComponent<Marker1Component>(builder.ReservedEntity));
        Assert.That(_entMan.HasComponent<Marker2Component>(builder.ReservedEntity));
        Assert.That(_entMan.HasComponent<Marker3Component>(builder.ReservedEntity));
        Assert.That(_entMan.HasComponent<Marker4Component>(builder.ReservedEntity));
    }
}
