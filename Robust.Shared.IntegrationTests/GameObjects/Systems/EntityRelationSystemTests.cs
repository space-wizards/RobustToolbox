using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.UnitTesting.Server;

namespace Robust.Shared.IntegrationTests.GameObjects.Systems;

[TestFixture, Parallelizable, TestOf(typeof(EntityRelation))]
internal sealed partial class EntityRelationSystemTests
{
    private const string RelationProto = "relationEnt";

    private const string Prototypes = $@"
- type: entity
  name: anchoredEnt
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
            Assert.That(testComp.Value.Entity, Is.EqualTo(targetEnt));
            Assert.That(testComp.List, Has.Count.EqualTo(1));
            Assert.That(testComp.Set, Has.Count.EqualTo(1));
            Assert.That(testComp.List, Does.Contain(new EntityRelation(targetEnt)));
            Assert.That(testComp.Set, Does.Contain(new EntityRelation(targetEnt)));

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
            Assert.That(testComp1.Value.Entity, Is.EqualTo(targetEnt));
            Assert.That(testComp1.List, Has.Count.EqualTo(1));
            Assert.That(testComp1.Set, Has.Count.EqualTo(1));
            Assert.That(testComp1.List, Does.Contain(new EntityRelation(targetEnt)));
            Assert.That(testComp1.Set, Does.Contain(new EntityRelation(targetEnt)));

            Assert.That(testComp2.Value.Entity, Is.EqualTo(targetEnt));
            Assert.That(testComp2.List, Has.Count.EqualTo(1));
            Assert.That(testComp2.Set, Has.Count.EqualTo(1));
            Assert.That(testComp2.List, Does.Contain(new EntityRelation(targetEnt)));
            Assert.That(testComp2.Set, Does.Contain(new EntityRelation(targetEnt)));

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
            Assert.That(testComp.Value.Entity, Is.Null);
            Assert.That(testComp.List, Is.Empty);
            Assert.That(testComp.Set, Is.Empty);
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
            Assert.That(testComp1.Value.Entity, Is.Null);
            Assert.That(testComp1.List, Is.Empty);
            Assert.That(testComp1.Set, Is.Empty);
            Assert.That(entMan.HasComponent<EntityRelationsComponent>(ownerEnt1), Is.False);

            Assert.That(testComp2.Value.Entity, Is.Null);
            Assert.That(testComp2.List, Is.Empty);
            Assert.That(testComp2.Set, Is.Empty);
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
            Assert.That(testComp2.Value.Entity, Is.EqualTo(targetEnt));
            Assert.That(testComp2.List, Has.Count.EqualTo(1));
            Assert.That(testComp2.Set, Has.Count.EqualTo(1));
            Assert.That(testComp2.List, Does.Contain(new EntityRelation(targetEnt)));
            Assert.That(testComp2.Set, Does.Contain(new EntityRelation(targetEnt)));

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
            Assert.That(testComp2.Value.Entity, Is.EqualTo(targetEnt));
            Assert.That(testComp2.List, Has.Count.EqualTo(1));
            Assert.That(testComp2.Set, Has.Count.EqualTo(1));
            Assert.That(testComp2.List, Does.Contain(new EntityRelation(targetEnt)));
            Assert.That(testComp2.Set, Does.Contain(new EntityRelation(targetEnt)));

            Assert.That(relationsComp2.Relations, Has.Count.EqualTo(EntityRelationsTestComponent.FieldCount));
            Assert.That(targetRelationsComp.Relations, Has.Count.EqualTo(EntityRelationsTestComponent.FieldCount));
        }

        entMan.RemoveComponent(ownerEnt2, testComp2);

        Assert.That(entMan.HasComponent<EntityRelationsComponent>(targetEnt), Is.False);
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
        entMan.SetRelation(ownerEnt, ref testComp.Value, targetEnt);
        entMan.SetRelation(ownerEnt, ref testComp.NullableValue, targetEnt);
        entMan.SetRelations(ownerEnt, testComp.List, [targetEnt]);
        entMan.SetRelations(ownerEnt, testComp.Set, [targetEnt]);
        entMan.SetRelations(ownerEnt, testComp.DictKey, new Dictionary<EntityUid, int> { [targetEnt] = 1 });
        entMan.SetRelations(ownerEnt, testComp.DictValue, new Dictionary<int, EntityUid> { [1] = targetEnt });
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

    [Reflect(false)]
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

    // This exists just because it's impossible to register auto-generated systems into the robust sim
    [Reflect(false)]
    private sealed partial class EntityRelationsTestSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<EntityRelationsTestComponent, ComponentShutdown>(OnRelationShutdown);
            SubscribeLocalEvent<EntityRelationsTestComponent, EntityRelationDeleteEvent>(OnRelationDeleted);
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

        private void OnRelationShutdown(Entity<EntityRelationsTestComponent> ent, ref ComponentShutdown args)
        {
            EntityRelationsTestComponent.ClearComponentRelations(ent, EntityManager);
        }
    }
}
