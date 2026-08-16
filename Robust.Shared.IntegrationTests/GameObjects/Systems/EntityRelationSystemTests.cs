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
        var sim = SimulationFactory();
        var entMan = sim.Resolve<IEntityManager>();

        var ownerEnt = entMan.Spawn(RelationProto);
        var targetEnt = entMan.Spawn();

        var testComp = entMan.GetComponent<EntityRelationsTestComponent>(ownerEnt);

        entMan.SetRelation(ownerEnt, ref testComp.Value, targetEnt);
        entMan.SetRelations(ownerEnt, testComp.List, [targetEnt]);
        entMan.SetRelations(ownerEnt, testComp.Set, [targetEnt]);

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
        var sim = SimulationFactory();
        var entMan = sim.Resolve<IEntityManager>();

        var ownerEnt1 = entMan.Spawn(RelationProto);
        var ownerEnt2 = entMan.Spawn(RelationProto);
        var targetEnt = entMan.Spawn();

        var testComp1 = entMan.GetComponent<EntityRelationsTestComponent>(ownerEnt1);
        var testComp2 = entMan.GetComponent<EntityRelationsTestComponent>(ownerEnt2);

        entMan.SetRelation(ownerEnt1, ref testComp1.Value, targetEnt);
        entMan.SetRelations(ownerEnt1, testComp1.List, [targetEnt]);
        entMan.SetRelations(ownerEnt1, testComp1.Set, [targetEnt]);
        entMan.SetRelation(ownerEnt2, ref testComp2.Value, targetEnt);
        entMan.SetRelations(ownerEnt2, testComp2.List, [targetEnt]);
        entMan.SetRelations(ownerEnt2, testComp2.Set, [targetEnt]);

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
        var sim = SimulationFactory();
        var entMan = sim.Resolve<IEntityManager>();

        var ownerEnt = entMan.Spawn(RelationProto);
        var targetEnt = entMan.Spawn();

        var testComp = entMan.GetComponent<EntityRelationsTestComponent>(ownerEnt);

        entMan.SetRelation(ownerEnt, ref testComp.Value, targetEnt);
        entMan.SetRelations(ownerEnt, testComp.List, [targetEnt]);
        entMan.SetRelations(ownerEnt, testComp.Set, [targetEnt]);

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
        var sim = SimulationFactory();
        var entMan = sim.Resolve<IEntityManager>();

        var ownerEnt1 = entMan.Spawn(RelationProto);
        var ownerEnt2 = entMan.Spawn(RelationProto);
        var targetEnt = entMan.Spawn();

        var testComp1 = entMan.GetComponent<EntityRelationsTestComponent>(ownerEnt1);
        var testComp2 = entMan.GetComponent<EntityRelationsTestComponent>(ownerEnt2);

        entMan.SetRelation(ownerEnt1, ref testComp1.Value, targetEnt);
        entMan.SetRelations(ownerEnt1, testComp1.List, [targetEnt]);
        entMan.SetRelations(ownerEnt1, testComp1.Set, [targetEnt]);
        entMan.SetRelation(ownerEnt2, ref testComp2.Value, targetEnt);
        entMan.SetRelations(ownerEnt2, testComp2.List, [targetEnt]);
        entMan.SetRelations(ownerEnt2, testComp2.Set, [targetEnt]);

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
        var sim = SimulationFactory();
        var entMan = sim.Resolve<IEntityManager>();

        var ownerEnt = entMan.Spawn(RelationProto);
        var targetEnt = entMan.Spawn();

        var testComp = entMan.GetComponent<EntityRelationsTestComponent>(ownerEnt);

        entMan.SetRelation(ownerEnt, ref testComp.Value, targetEnt);
        entMan.SetRelations(ownerEnt, testComp.List, [targetEnt]);
        entMan.SetRelations(ownerEnt, testComp.Set, [targetEnt]);

        entMan.DeleteEntity(ownerEnt);

        Assert.That(entMan.HasComponent<EntityRelationsComponent>(targetEnt), Is.False);
    }

    /// <summary>
    /// Set relations between 2 owner and 1 target entities and deletes the owner entities one-by-one.
    /// The test plan is:
    /// <list type="number">
    /// <item>A target entity is assigned to a field in the owner component</item>
    /// <item>EntityRelationsComponent was added to both entities</item>
    /// <item>The first owner entity is deleted</item>
    /// <item>The target has half the references, second owner is unchanged</item>
    /// <item>The second owner entity is deleted</item>
    /// <item>EntityRelationsComponent was removed from the target</item>
    /// </list>
    /// </summary>
    [Test]
    public void Relation_DeleteOwnersMany_Test()
    {
        var sim = SimulationFactory();
        var entMan = sim.Resolve<IEntityManager>();

        var ownerEnt1 = entMan.Spawn(RelationProto);
        var ownerEnt2 = entMan.Spawn(RelationProto);
        var targetEnt = entMan.Spawn();

        var testComp1 = entMan.GetComponent<EntityRelationsTestComponent>(ownerEnt1);
        var testComp2 = entMan.GetComponent<EntityRelationsTestComponent>(ownerEnt2);

        entMan.SetRelation(ownerEnt1, ref testComp1.Value, targetEnt);
        entMan.SetRelations(ownerEnt1, testComp1.List, [targetEnt]);
        entMan.SetRelations(ownerEnt1, testComp1.Set, [targetEnt]);
        entMan.SetRelation(ownerEnt2, ref testComp2.Value, targetEnt);
        entMan.SetRelations(ownerEnt2, testComp2.List, [targetEnt]);
        entMan.SetRelations(ownerEnt2, testComp2.Set, [targetEnt]);

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
    /// <item>An entity is assigned to a field in the component, EntityRelationsComponent was added to both ents</item>
    /// <item>The related entity's component that stores the reference is removed</item>
    /// <item>EntityRelationsComponent was removed both from the entity and the target</item>
    /// </list>
    /// </summary>
    [Test]
    public void Relation_RemoveTestComponent_Test()
    {
        var sim = SimulationFactory();
        var entMan = sim.Resolve<IEntityManager>();

        var ent = entMan.Spawn(RelationProto);
        var targetEnt = entMan.Spawn();

        var testComp = entMan.GetComponent<EntityRelationsTestComponent>(ent);

        entMan.SetRelation(ent, ref testComp.Value, targetEnt);
        entMan.SetRelations(ent, testComp.List, [targetEnt]);
        entMan.SetRelations(ent, testComp.Set, [targetEnt]);

        entMan.RemoveComponent<EntityRelationsTestComponent>(ent);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entMan.HasComponent<EntityRelationsComponent>(ent), Is.False);
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
        var sim = SimulationFactory();
        var entMan = sim.Resolve<IEntityManager>();

        var ownerEnt1 = entMan.Spawn(RelationProto);
        var ownerEnt2 = entMan.Spawn(RelationProto);
        var targetEnt = entMan.Spawn();

        var testComp1 = entMan.GetComponent<EntityRelationsTestComponent>(ownerEnt1);
        var testComp2 = entMan.GetComponent<EntityRelationsTestComponent>(ownerEnt2);

        entMan.SetRelation(ownerEnt1, ref testComp1.Value, targetEnt);
        entMan.SetRelations(ownerEnt1, testComp1.List, [targetEnt]);
        entMan.SetRelations(ownerEnt1, testComp1.Set, [targetEnt]);
        entMan.SetRelation(ownerEnt2, ref testComp2.Value, targetEnt);
        entMan.SetRelations(ownerEnt2, testComp2.List, [targetEnt]);
        entMan.SetRelations(ownerEnt2, testComp2.Set, [targetEnt]);

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

    private static ISimulation SimulationFactory()
    {
        var sim = RobustServerSimulation
            .NewSimulation()
            .RegisterEntitySystems(f =>
            {
                f.LoadExtraSystemType<EntityRelationsTestComponent.EntityRelationsTestComponent_AutoRelationsSystem>();
            })
            .RegisterComponents(f => f.RegisterClass<EntityRelationsTestComponent>())
            .RegisterPrototypes(f => f.LoadString(Prototypes))
            .InitializeInstance();

        return sim;
    }

    [Reflect(false), AutoGenerateEntityRelations]
    private sealed partial class EntityRelationsTestComponent : Component
    {
        public const int FieldCount = 3;

        [DataField, AutoRelationField]
        public EntityRelation Value;

        [DataField, AutoRelationField]
        public List<EntityRelation> List = new();

        [DataField, AutoRelationField]
        public HashSet<EntityRelation> Set = new();
    }
}
