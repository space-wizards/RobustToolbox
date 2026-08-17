using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.UnitTesting;
using Robust.UnitTesting.Server;

namespace Robust.Shared.IntegrationTests.GameObjects.Systems;

[TestFixture, Parallelizable, TestOf(typeof(EntityRelation))]
internal sealed partial class EntityRelationsSystemTests : RobustIntegrationTest
{
    private const string RelationProto = "relationEnt";

    private const string Prototypes = $@"
- type: entity
  name: relationEnt
  id: {RelationProto}
  components:
  - type: EntityRelationsTest";

    /// <summary>
    /// Sets relations between two entities and checks if the setup is correct.
    /// </summary>
    [Test]
    public void Relation_SetRelation_Test()
    {
        Setup(out var entMan, out var ownerEnt, out var testComp, out var targetEnt);

        SetRelations(ownerEnt, testComp, targetEnt, entMan);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entMan.HasComponent<EntityRelationsComponent>(ownerEnt));
            Assert.That(entMan.HasComponent<EntityRelationsComponent>(targetEnt));
        }

        var relationsComp = entMan.GetComponent<EntityRelationsComponent>(ownerEnt);
        var targetRelationsComp = entMan.GetComponent<EntityRelationsComponent>(targetEnt);

        using (Assert.EnterMultipleScope())
        {
            AssertTestCompTarget(testComp, targetEnt);

            Assert.That(relationsComp.Relations, Has.Count.EqualTo(EntityRelationsTestComponent.FieldCount));
            Assert.That(targetRelationsComp.Relations, Has.Count.EqualTo(EntityRelationsTestComponent.FieldCount));
        }
    }

    /// <summary>
    /// Sets relations between a single target and multiple owners and checks if the setup is correct.
    /// </summary>
    [Test]
    public void Relation_SetRelationsMany_Test()
    {
        SetupMany(
            out var entMan,
            out var ownerEnt1,
            out var testComp1,
            out var ownerEnt2,
            out var testComp2,
            out var targetEnt);

        SetRelations(ownerEnt1, testComp1, targetEnt, entMan);
        SetRelations(ownerEnt2, testComp2, targetEnt, entMan);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entMan.HasComponent<EntityRelationsComponent>(ownerEnt1));
            Assert.That(entMan.HasComponent<EntityRelationsComponent>(ownerEnt2));
            Assert.That(entMan.HasComponent<EntityRelationsComponent>(targetEnt));
        }

        var relationsComp1 = entMan.GetComponent<EntityRelationsComponent>(ownerEnt1);
        var relationsComp2 = entMan.GetComponent<EntityRelationsComponent>(ownerEnt2);
        var targetRelationsComp = entMan.GetComponent<EntityRelationsComponent>(targetEnt);

        using (Assert.EnterMultipleScope())
        {
            AssertTestCompTarget(testComp1, targetEnt);
            AssertTestCompTarget(testComp2, targetEnt);

            Assert.That(relationsComp1.Relations, Has.Count.EqualTo(EntityRelationsTestComponent.FieldCount));
            Assert.That(relationsComp2.Relations, Has.Count.EqualTo(EntityRelationsTestComponent.FieldCount));
            Assert.That(targetRelationsComp.Relations, Has.Count.EqualTo(EntityRelationsTestComponent.FieldCount * 2));
        }
    }

    /// <summary>
    /// Set relations between two entities and deletes the target entity.
    /// The test plan is:
    /// <list type="number">
    /// <item>An entity is assigned to a field in the component</item>
    /// <item>EntityRelationsComponent was added to all entities</item>
    /// <item>The related entity is deleted</item>
    /// <item>Component field is now empty, EntityRelationsComponent was removed from the owner</item>
    /// </list>
    /// </summary>
    [Test]
    public void Relation_DeleteTarget_Test()
    {
        Setup(out var entMan, out var ownerEnt, out var testComp, out var targetEnt);

        SetRelations(ownerEnt, testComp, targetEnt, entMan);

        entMan.DeleteEntity(targetEnt);

        using (Assert.EnterMultipleScope())
        {
            AssertTestCompEmpty(testComp);

            Assert.That(entMan.HasComponent<EntityRelationsComponent>(ownerEnt), Is.False);
        }
    }

    /// <summary>
    /// Set relations between 2 owner and 1 target entities and deletes the target entity.
    /// The test plan is:
    /// <list type="number">
    /// <item>An entity is assigned to a field in 2 test components</item>
    /// <item>EntityRelationsComponent was added to all entities</item>
    /// <item>The target entity is deleted</item>
    /// <item>Component fields are now empty, EntityRelationsComponent was removed from both owners</item>
    /// </list>
    /// </summary>
    [Test]
    public void Relation_DeleteTargetMany_Test()
    {
        SetupMany(
            out var entMan,
            out var ownerEnt1,
            out var testComp1,
            out var ownerEnt2,
            out var testComp2,
            out var targetEnt);

        SetRelations(ownerEnt1, testComp1, targetEnt, entMan);
        SetRelations(ownerEnt2, testComp2, targetEnt, entMan);

        entMan.DeleteEntity(targetEnt);

        using (Assert.EnterMultipleScope())
        {
            AssertTestCompEmpty(testComp1);
            AssertTestCompEmpty(testComp2);

            Assert.That(entMan.HasComponent<EntityRelationsComponent>(ownerEnt2), Is.False);
        }
    }

    /// <summary>
    /// Set relations between two entities and deletes the owner entity.
    /// The test plan is:
    /// <list type="number">
    /// <item>A target entity is assigned to a field in the owner component</item>
    /// <item>EntityRelationsComponent was added to both entities</item>
    /// <item>The owner entity is deleted</item>
    /// <item>EntityRelationsComponent was removed from the target</item>
    /// </list>
    /// </summary>
    [Test]
    public void Relation_DeleteOwner_Test()
    {
        Setup(out var entMan, out var ownerEnt, out var testComp, out var targetEnt);

        SetRelations(ownerEnt, testComp, targetEnt, entMan);

        entMan.DeleteEntity(ownerEnt);

        Assert.That(entMan.HasComponent<EntityRelationsComponent>(targetEnt), Is.False);
    }

    /// <summary>
    /// Set relations between 2 owner and 1 target entities and deletes the owner entities one-by-one.
    /// The test plan is:
    /// <list type="number">
    /// <item>A target entity is assigned to a field in the owner component</item>
    /// <item>EntityRelationsComponent was added to all entities</item>
    /// <item>The first owner entity is deleted</item>
    /// <item>The target has half the references, second owner is unchanged</item>
    /// <item>The second owner entity is deleted</item>
    /// <item>EntityRelationsComponent was removed from the target</item>
    /// </list>
    /// </summary>
    [Test]
    public void Relation_DeleteOwnersMany_Test()
    {
        SetupMany(
            out var entMan,
            out var ownerEnt1,
            out var testComp1,
            out var ownerEnt2,
            out var testComp2,
            out var targetEnt);

        SetRelations(ownerEnt1, testComp1, targetEnt, entMan);
        SetRelations(ownerEnt2, testComp2, targetEnt, entMan);

        entMan.DeleteEntity(ownerEnt1);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entMan.HasComponent<EntityRelationsComponent>(targetEnt), Is.True);
            Assert.That(entMan.HasComponent<EntityRelationsComponent>(ownerEnt2), Is.True);
        }

        var relationsComp2 = entMan.GetComponent<EntityRelationsComponent>(ownerEnt2);
        var targetRelationsComp = entMan.GetComponent<EntityRelationsComponent>(targetEnt);

        using (Assert.EnterMultipleScope())
        {
            AssertTestCompTarget(testComp2, targetEnt);

            Assert.That(relationsComp2.Relations, Has.Count.EqualTo(EntityRelationsTestComponent.FieldCount));
            Assert.That(targetRelationsComp.Relations, Has.Count.EqualTo(EntityRelationsTestComponent.FieldCount));
        }

        entMan.DeleteEntity(ownerEnt2);

        Assert.That(entMan.HasComponent<EntityRelationsComponent>(targetEnt), Is.False);
    }

    /// <summary>
    /// Set relations between two entities and deletes the test component that referenced the target.
    /// The test plan is:
    /// <list type="number">
    /// <item>An entity is assigned to a field in the component</item>
    /// <item>EntityRelationsComponent was added to both entities</item>
    /// <item>The related entity's component that stores the reference is removed</item>
    /// <item>EntityRelationsComponent was removed both from the entity and the target</item>
    /// </list>
    /// </summary>
    [Test]
    public void Relation_RemoveTestComponent_Test()
    {
        Setup(out var entMan, out var ownerEnt, out var testComp, out var targetEnt);

        SetRelations(ownerEnt, testComp, targetEnt, entMan);

        entMan.RemoveComponent<EntityRelationsTestComponent>(ownerEnt);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entMan.HasComponent<EntityRelationsComponent>(ownerEnt), Is.False);
            Assert.That(entMan.HasComponent<EntityRelationsComponent>(targetEnt), Is.False);
        }
    }

    /// <summary>
    /// Set relations between 2 owner entities and 1 target and deletes
    /// the test component that referenced the target on each owner one-by-one.
    /// The test plan is:
    /// <list type="number">
    /// <item>An entity is assigned to a field in the component, EntityRelationsComponent was added to all ents</item>
    /// <item>The first owner entity's component that stores the reference is removed</item>
    /// <item>Half of the relations are removed on the target, Second owner is unchanged</item>
    /// <item>The second owner entity's component that stores the reference is removed</item>
    /// <item>EntityRelationsComponent was removed from all entities</item>
    /// </list>
    /// </summary>
    [Test]
    public void Relation_RemoveTestComponentsMany_Test()
    {
        SetupMany(
            out var entMan,
            out var ownerEnt1,
            out var testComp1,
            out var ownerEnt2,
            out var testComp2,
            out var targetEnt);

        SetRelations(ownerEnt1, testComp1, targetEnt, entMan);
        SetRelations(ownerEnt2, testComp2, targetEnt, entMan);

        entMan.RemoveComponent(ownerEnt1, testComp1);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entMan.HasComponent<EntityRelationsComponent>(targetEnt), Is.True);
            Assert.That(entMan.HasComponent<EntityRelationsComponent>(ownerEnt2), Is.True);
        }

        var relationsComp2 = entMan.GetComponent<EntityRelationsComponent>(ownerEnt2);
        var targetRelationsComp = entMan.GetComponent<EntityRelationsComponent>(targetEnt);

        using (Assert.EnterMultipleScope())
        {
            AssertTestCompTarget(testComp2, targetEnt);

            Assert.That(relationsComp2.Relations, Has.Count.EqualTo(EntityRelationsTestComponent.FieldCount));
            Assert.That(targetRelationsComp.Relations, Has.Count.EqualTo(EntityRelationsTestComponent.FieldCount));
        }

        entMan.RemoveComponent(ownerEnt2, testComp2);

        Assert.That(entMan.HasComponent<EntityRelationsComponent>(targetEnt), Is.False);
    }

    /// <summary>
    /// Set relations between two entities and deletes the test component that referenced the target.
    /// The test plan is:
    /// <list type="number">
    /// <item>An entity is assigned to a field in the component</item>
    /// <item>EntityRelationsComponent was added to both entities</item>
    /// <item>The owner's <see cref="EntityRelationsComponent"/> that stores the reference is removed</item>
    /// <item><see cref="EntityRelationsComponent"/> was removed from the target</item>
    /// <item>Test component is clear from any EntityRelation references</item>
    /// </list>
    /// </summary>
    [Test]
    public void Relation_RemoveRelationsComponent_Test()
    {
        Setup(out var entMan, out var ownerEnt, out var testComp, out var targetEnt);

        SetRelations(ownerEnt, testComp, targetEnt, entMan);

        entMan.RemoveComponent<EntityRelationsComponent>(ownerEnt);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entMan.HasComponent<EntityRelationsComponent>(targetEnt), Is.False);

            AssertTestCompEmpty(testComp);
        }
    }

    /// <summary>
    /// Set relations between 2 owner entities and 1 target and deletes
    /// the test component that referenced the target on each owner one-by-one.
    /// The test plan is:
    /// <list type="number">
    /// <item>An entity is assigned to a field in the component, EntityRelationsComponent was added to all ents</item>
    /// <item>The first owner entity's component that stores the reference is removed</item>
    /// <item>Half of the relations are removed on the target, Second owner is unchanged</item>
    /// <item>The second owner entity's component that stores the reference is removed</item>
    /// <item>EntityRelationsComponent was removed from all entities</item>
    /// </list>
    /// </summary>
    [Test]
    public void Relation_RemoveRelationsComponentsMany_Test()
    {
        SetupMany(
            out var entMan,
            out var ownerEnt1,
            out var testComp1,
            out var ownerEnt2,
            out var testComp2,
            out var targetEnt);

        SetRelations(ownerEnt1, testComp1, targetEnt, entMan);
        SetRelations(ownerEnt2, testComp2, targetEnt, entMan);

        entMan.RemoveComponent<EntityRelationsComponent>(ownerEnt1);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entMan.HasComponent<EntityRelationsComponent>(targetEnt), Is.True);
            Assert.That(entMan.HasComponent<EntityRelationsComponent>(ownerEnt2), Is.True);
        }

        var relationsComp2 = entMan.GetComponent<EntityRelationsComponent>(ownerEnt2);
        var targetRelationsComp = entMan.GetComponent<EntityRelationsComponent>(targetEnt);

        using (Assert.EnterMultipleScope())
        {
            AssertTestCompEmpty(testComp1);
            AssertTestCompTarget(testComp2, targetEnt);

            Assert.That(relationsComp2.Relations, Has.Count.EqualTo(EntityRelationsTestComponent.FieldCount));
            Assert.That(targetRelationsComp.Relations, Has.Count.EqualTo(EntityRelationsTestComponent.FieldCount));
        }

        entMan.RemoveComponent<EntityRelationsComponent>(ownerEnt2);

        using (Assert.EnterMultipleScope())
        {
            AssertTestCompEmpty(testComp2);

            Assert.That(entMan.HasComponent<EntityRelationsComponent>(targetEnt), Is.False);
        }
    }

    private static void Setup(
        out IEntityManager entMan,
        out EntityUid ownerEnt,
        out EntityRelationsTestComponent testComp,
        out EntityUid targetEnt)
    {
        entMan = SimulationFactory().Resolve<IEntityManager>();
        ownerEnt = entMan.Spawn(RelationProto);
        targetEnt = entMan.Spawn();
        testComp = entMan.GetComponent<EntityRelationsTestComponent>(ownerEnt);
    }

    private static void SetupMany(
        out IEntityManager entMan,
        out EntityUid ownerEnt1,
        out EntityRelationsTestComponent testComp1,
        out EntityUid ownerEnt2,
        out EntityRelationsTestComponent testComp2,
        out EntityUid targetEnt)
    {
        entMan = SimulationFactory().Resolve<IEntityManager>();
        ownerEnt1 = entMan.Spawn(RelationProto);
        ownerEnt2 = entMan.Spawn(RelationProto);
        targetEnt = entMan.Spawn();
        testComp1 = entMan.GetComponent<EntityRelationsTestComponent>(ownerEnt1);
        testComp2 = entMan.GetComponent<EntityRelationsTestComponent>(ownerEnt2);
    }

    private static void SetRelations(EntityUid ownerEnt, EntityRelationsTestComponent testComp, EntityUid targetEnt, IEntityManager entMan)
    {
        entMan.SetRelation(ownerEnt, ref testComp.Value, targetEnt, false);
        entMan.SetRelation(ownerEnt, ref testComp.NullableValue, targetEnt, false);
        entMan.SetRelations(ownerEnt, testComp.List, [targetEnt], false);
        entMan.SetRelations(ownerEnt, testComp.Set, [targetEnt], false);
        entMan.SetRelations(ownerEnt, testComp.DictKey, new Dictionary<EntityUid, int> { [targetEnt] = 1 }, false);
        entMan.SetRelations(ownerEnt, testComp.DictValue, new Dictionary<int, EntityUid> { [testComp.DictValue.Count + 1] = targetEnt }, false);
    }

    private static void AssertTestCompTarget(EntityRelationsTestComponent testComp, EntityUid targetEnt)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(testComp.Value.Entity, Is.EqualTo(targetEnt));
            Assert.That(testComp.NullableValue?.Entity, Is.EqualTo(targetEnt));
            Assert.That(testComp.List, Has.Count.EqualTo(1));
            Assert.That(testComp.Set, Has.Count.EqualTo(1));
            Assert.That(testComp.List, Does.Contain(new EntityRelation(targetEnt)));
            Assert.That(testComp.Set, Does.Contain(new EntityRelation(targetEnt)));
            Assert.That(testComp.DictKey, Has.Count.EqualTo(1));
            Assert.That(testComp.DictValue, Has.Count.EqualTo(1));
            Assert.That(testComp.DictKey, Does.ContainKey(new EntityRelation(targetEnt)));
            Assert.That(testComp.DictValue, Does.ContainValue(new EntityRelation(targetEnt)));
        }
    }

    private static void AssertTestCompEmpty(EntityRelationsTestComponent testComp)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(testComp.Value.Entity, Is.Null);
            Assert.That(testComp.NullableValue, Is.Null);
            Assert.That(testComp.List, Is.Empty);
            Assert.That(testComp.Set, Is.Empty);
            Assert.That(testComp.DictKey, Is.Empty);
            Assert.That(testComp.DictValue,
                Is.EquivalentTo(new Dictionary<int, EntityRelation>
                {
                    [1] = EntityRelation.Null
                }));
        }
    }

    private static ISimulation SimulationFactory()
    {
        var sim = RobustServerSimulation
            .NewSimulation()
            .RegisterEntitySystems(f =>
            {
                f.LoadExtraSystemType<EntityRelationsTestSystem>();
            })
            .RegisterComponents(f => f.RegisterClass<EntityRelationsTestComponent>())
            .RegisterPrototypes(f => f.LoadString(Prototypes))
            .InitializeInstance();

        return sim;
    }

    [Reflect(false), NetworkedComponent]
    private sealed partial class EntityRelationsTestComponent : Component
    {
        public const int FieldCount = 6;

        [DataField]
        public EntityRelation Value;

        [DataField]
        public EntityRelation? NullableValue;

        [DataField]
        public List<EntityRelation> List = new();

        [DataField]
        public HashSet<EntityRelation> Set = new();

        [DataField]
        public Dictionary<EntityRelation, int> DictKey = new();

        [DataField]
        public Dictionary<int, EntityRelation> DictValue = new();

        /// <summary>
        /// Auto-generated method that clears all relations in a certain entity.
        /// This has to be called on component shutdown to keep all relations correct.
        /// </summary>
        public static void ClearComponentRelations(Entity<EntityRelationsTestComponent> ent, IEntityManager entMan)
        {
            entMan.ClearRelation(ent.Owner, ref ent.Comp.Value);
            entMan.ClearRelation(ent.Owner, ref ent.Comp.NullableValue);
            entMan.ClearRelation(ent.Owner, ent.Comp.List);
            entMan.ClearRelation(ent.Owner, ent.Comp.Set);
            entMan.ClearRelation(ent.Owner, ent.Comp.DictKey);
            entMan.ClearRelation(ent.Owner, ent.Comp.DictValue);
        }
    }

    [Serializable, NetSerializable]
    private sealed partial class EntityRelationsTestComponentState : IComponentState
    {
        public NetEntity? Value = default!;
        public NetEntity? NullableValue = default!;
        public List<NetEntity> List = default!;
        public HashSet<NetEntity> Set = default!;
        public Dictionary<NetEntity, int> DictKey = default!;
        public Dictionary<int, NetEntity?> DictValue = default!;
    }

    // This exists just because auto-generated code doesn't work for private test components and systems
    [Reflect(false)]
    private sealed partial class EntityRelationsTestSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<EntityRelationsTestComponent, ComponentShutdown>(OnRelationShutdown);
            SubscribeLocalEvent<EntityRelationsTestComponent, EntityRelationDeleteEvent>(OnRelationDeleted);
            SubscribeLocalEvent<EntityRelationsTestComponent, EntityRelationShutdownEvent>(OnRelationsClear);
            SubscribeLocalEvent<EntityRelationsTestComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<EntityRelationsTestComponent, ComponentHandleState>(OnHandleState);
        }

        private void OnRelationDeleted(Entity<EntityRelationsTestComponent> ent, ref EntityRelationDeleteEvent args)
        {
            if (ent.Comp.Value == args.Relation)
                ent.Comp.Value = EntityRelation.Null;
            if (ent.Comp.NullableValue.HasValue && ent.Comp.NullableValue.Value == args.Relation)
                ent.Comp.NullableValue = null;
            ent.Comp.List.Remove(args.Relation);
            ent.Comp.Set.Remove(args.Relation);
            ent.Comp.DictKey.Remove(args.Relation);
            foreach (var (key, value) in ent.Comp.DictValue)
            {
                if (value == args.Relation)
                    ent.Comp.DictValue[key] = EntityRelation.Null;
            }
        }

        private void OnRelationsClear(Entity<EntityRelationsTestComponent> ent, ref EntityRelationShutdownEvent args)
        {
            ent.Comp.Value = EntityRelation.Null;
            ent.Comp.NullableValue = null;
            ent.Comp.List.Clear();
            ent.Comp.Set.Clear();
            ent.Comp.DictKey.Clear();
            foreach (var key in ent.Comp.DictValue.Keys)
            {
                ent.Comp.DictValue[key] = EntityRelation.Null;
            }
        }

        private void OnRelationShutdown(Entity<EntityRelationsTestComponent> ent, ref ComponentShutdown args)
        {
            EntityRelationsTestComponent.ClearComponentRelations(ent, EntityManager);
        }

        private void OnGetState(EntityUid uid, EntityRelationsTestComponent component, ref ComponentGetState args)
        {
            // Get full state
            args.State = new EntityRelationsTestComponentState
            {
                Value = GetNetEntity(component.Value),
                NullableValue = GetNetEntity(component.NullableValue),
                List = GetNetEntityList(component.List),
                Set = GetNetEntitySet(component.Set),
                DictKey = GetNetEntityDictionary(component.DictKey),
                DictValue = GetNetEntityDictionary(component.DictValue),
            };
        }

        private void OnHandleState(EntityUid uid, EntityRelationsTestComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not EntityRelationsTestComponentState state)
                return;

            component.Value = EnsureEntityRelation<EntityRelationsTestComponent>(state.Value, uid);
            component.NullableValue = EnsureEntityRelation<EntityRelationsTestComponent>(state.NullableValue, uid);
            EnsureEntityListRelation<EntityRelationsTestComponent>(state.List, uid, component.List);
            EnsureEntitySetRelation<EntityRelationsTestComponent>(state.Set, uid, component.Set);
            EnsureEntityDictionary<EntityRelationsTestComponent, int>(state.DictKey, uid, component.DictKey);
            EnsureEntityDictionary<EntityRelationsTestComponent, int>(state.DictValue, uid, component.DictValue);
        }
    }
}
