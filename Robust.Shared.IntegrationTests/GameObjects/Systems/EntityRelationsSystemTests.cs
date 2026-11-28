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

    /// <summary>
    /// Sets a relation between the owner and the first target, then sets a relation between owner and the second target,
    /// then checks if the relation was properly cut between the owner and the first target.
    /// </summary>
    [Test]
    public void Relation_ReplaceRelation_Test()
    {
        Setup(out var entMan, out var ownerEnt, out var testComp, out var targetEnt1);
        var targetEnt2 = entMan.Spawn();

        SetRelations(ownerEnt, testComp, targetEnt1, entMan);

        // Collections have to be cleared manually since there's no way to know which entity is being replaced.
        entMan.ClearRelation(ownerEnt, testComp.List, targetEnt1);
        entMan.ClearRelation(ownerEnt, testComp.Set, targetEnt1);
        entMan.ClearRelation(ownerEnt, testComp.DictKey, targetEnt1);
        entMan.ClearRelation(ownerEnt, testComp.DictValue, targetEnt1, true);

        SetRelations(ownerEnt, testComp, targetEnt2, entMan);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entMan.HasComponent<EntityRelationsComponent>(ownerEnt), Is.True);
            Assert.That(entMan.HasComponent<EntityRelationsComponent>(targetEnt2), Is.True);
            Assert.That(entMan.HasComponent<EntityRelationsComponent>(targetEnt1), Is.False);
        }

        var relationsComp = entMan.GetComponent<EntityRelationsComponent>(ownerEnt);
        var targetRelationsComp = entMan.GetComponent<EntityRelationsComponent>(targetEnt2);

        using (Assert.EnterMultipleScope())
        {
            AssertTestCompTarget(testComp, targetEnt2);

            Assert.That(relationsComp.Relations, Has.Count.EqualTo(EntityRelationsTestComponent.FieldCount));
            Assert.That(targetRelationsComp.Relations, Has.Count.EqualTo(EntityRelationsTestComponent.FieldCount));
        }
    }

    /// <summary>
    /// Sets relations between two entities and checks if the setup is correct.
    /// </summary>
    [Test]
    public void Relation_Local_SetRelation_Test()
    {
        Setup(out var entMan, out var ownerEnt, out var testComp, out var targetEnt);

        SetLocalRelations(ownerEnt, testComp, targetEnt, entMan);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entMan.HasComponent<EntityRelationsComponent>(ownerEnt));
            Assert.That(entMan.HasComponent<EntityRelationsComponent>(targetEnt));
        }

        var relationsComp = entMan.GetComponent<EntityRelationsComponent>(ownerEnt);
        var targetRelationsComp = entMan.GetComponent<EntityRelationsComponent>(targetEnt);

        using (Assert.EnterMultipleScope())
        {
            AssertLocalTestCompTarget(testComp, targetEnt);

            Assert.That(relationsComp.LocalRelations, Has.Count.EqualTo(EntityRelationsTestComponent.FieldCount));
            Assert.That(targetRelationsComp.LocalRelations, Has.Count.EqualTo(EntityRelationsTestComponent.FieldCount));
        }
    }

    /// <summary>
    /// Sets relations between a single target and multiple owners and checks if the setup is correct.
    /// </summary>
    [Test]
    public void Relation_Local_SetRelationsMany_Test()
    {
        SetupMany(
            out var entMan,
            out var ownerEnt1,
            out var testComp1,
            out var ownerEnt2,
            out var testComp2,
            out var targetEnt);

        SetLocalRelations(ownerEnt1, testComp1, targetEnt, entMan);
        SetLocalRelations(ownerEnt2, testComp2, targetEnt, entMan);

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
            AssertLocalTestCompTarget(testComp1, targetEnt);
            AssertLocalTestCompTarget(testComp2, targetEnt);

            Assert.That(relationsComp1.LocalRelations, Has.Count.EqualTo(EntityRelationsTestComponent.FieldCount));
            Assert.That(relationsComp2.LocalRelations, Has.Count.EqualTo(EntityRelationsTestComponent.FieldCount));
            Assert.That(targetRelationsComp.LocalRelations, Has.Count.EqualTo(EntityRelationsTestComponent.FieldCount * 2));
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
    public void Relation_Local_DeleteTarget_Test()
    {
        Setup(out var entMan, out var ownerEnt, out var testComp, out var targetEnt);

        SetLocalRelations(ownerEnt, testComp, targetEnt, entMan);

        entMan.DeleteEntity(targetEnt);

        using (Assert.EnterMultipleScope())
        {
            AssertLocalTestCompEmpty(testComp);

            Assert.That(entMan.HasComponent<EntityRelationsComponent>(ownerEnt), Is.True);
            Assert.That(entMan.GetComponent<EntityRelationsComponent>(ownerEnt).LocalRelations, Has.Count.EqualTo(0));
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
    public void Relation_Local_DeleteTargetMany_Test()
    {
        SetupMany(
            out var entMan,
            out var ownerEnt1,
            out var testComp1,
            out var ownerEnt2,
            out var testComp2,
            out var targetEnt);

        SetLocalRelations(ownerEnt1, testComp1, targetEnt, entMan);
        SetLocalRelations(ownerEnt2, testComp2, targetEnt, entMan);

        entMan.DeleteEntity(targetEnt);

        using (Assert.EnterMultipleScope())
        {
            AssertLocalTestCompEmpty(testComp1);
            AssertLocalTestCompEmpty(testComp2);

            Assert.That(entMan.HasComponent<EntityRelationsComponent>(ownerEnt1), Is.True);
            Assert.That(entMan.GetComponent<EntityRelationsComponent>(ownerEnt1).LocalRelations, Has.Count.EqualTo(0));
            Assert.That(entMan.HasComponent<EntityRelationsComponent>(ownerEnt2), Is.True);
            Assert.That(entMan.GetComponent<EntityRelationsComponent>(ownerEnt2).LocalRelations, Has.Count.EqualTo(0));
        }
    }

    /// <summary>
    /// Set relations between two entities and deletes the owner entity.
    /// The test plan is:
    /// <list type="number">
    /// <item>A target entity is assigned to a field in the owner component</item>
    /// <item>EntityRelationsComponent was added to both entities</item>
    /// <item>The owner entity is deleted</item>
    /// <item>target's EntityRelationsComponent is empty</item>
    /// </list>
    /// </summary>
    [Test]
    public void Relation_Local_DeleteOwner_Test()
    {
        Setup(out var entMan, out var ownerEnt, out var testComp, out var targetEnt);

        SetLocalRelations(ownerEnt, testComp, targetEnt, entMan);

        entMan.DeleteEntity(ownerEnt);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entMan.HasComponent<EntityRelationsComponent>(targetEnt), Is.True);
            Assert.That(entMan.GetComponent<EntityRelationsComponent>(targetEnt).LocalRelations, Has.Count.EqualTo(0));
        }
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
    public void Relation_Local_DeleteOwnersMany_Test()
    {
        SetupMany(
            out var entMan,
            out var ownerEnt1,
            out var testComp1,
            out var ownerEnt2,
            out var testComp2,
            out var targetEnt);

        SetLocalRelations(ownerEnt1, testComp1, targetEnt, entMan);
        SetLocalRelations(ownerEnt2, testComp2, targetEnt, entMan);

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
            AssertLocalTestCompTarget(testComp2, targetEnt);

            Assert.That(relationsComp2.LocalRelations, Has.Count.EqualTo(EntityRelationsTestComponent.FieldCount));
            Assert.That(targetRelationsComp.LocalRelations, Has.Count.EqualTo(EntityRelationsTestComponent.FieldCount));
        }

        entMan.DeleteEntity(ownerEnt2);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entMan.HasComponent<EntityRelationsComponent>(targetEnt), Is.True);
            Assert.That(entMan.GetComponent<EntityRelationsComponent>(targetEnt).LocalRelations, Has.Count.EqualTo(0));
        }
    }

    /// <summary>
    /// Sets a relation between the owner and the first target, then sets a relation between owner and the second target,
    /// then checks if the relation was properly cut between the owner and the first target.
    /// </summary>
    [Test]
    public void Relation_Local_ReplaceRelation_Test()
    {
        Setup(out var entMan, out var ownerEnt, out var testComp, out var targetEnt1);
        var targetEnt2 = entMan.Spawn();

        SetLocalRelations(ownerEnt, testComp, targetEnt1, entMan);

        // Collections have to be cleared manually since there's no way to know which entity is being replaced.
        entMan.ClearLocalRelation(ownerEnt, testComp.LocalList, targetEnt1);
        entMan.ClearLocalRelation(ownerEnt, testComp.LocalSet, targetEnt1);
        entMan.ClearLocalRelation(ownerEnt, testComp.LocalDictKey, targetEnt1);
        entMan.ClearLocalRelation(ownerEnt, testComp.LocalDictValue, targetEnt1, true);

        SetLocalRelations(ownerEnt, testComp, targetEnt2, entMan);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entMan.HasComponent<EntityRelationsComponent>(ownerEnt), Is.True);
            Assert.That(entMan.HasComponent<EntityRelationsComponent>(targetEnt2), Is.True);
            Assert.That(entMan.HasComponent<EntityRelationsComponent>(targetEnt1), Is.True);
            Assert.That(entMan.GetComponent<EntityRelationsComponent>(targetEnt1).LocalRelations, Has.Count.EqualTo(0));
        }

        var relationsComp = entMan.GetComponent<EntityRelationsComponent>(ownerEnt);
        var targetRelationsComp = entMan.GetComponent<EntityRelationsComponent>(targetEnt2);

        using (Assert.EnterMultipleScope())
        {
            AssertLocalTestCompTarget(testComp, targetEnt2);

            Assert.That(relationsComp.LocalRelations, Has.Count.EqualTo(EntityRelationsTestComponent.FieldCount));
            Assert.That(targetRelationsComp.LocalRelations, Has.Count.EqualTo(EntityRelationsTestComponent.FieldCount));
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
        entMan.SetRelation(ownerEnt, ref testComp.Value, targetEnt);
        entMan.SetRelation(ownerEnt, ref testComp.NullableValue, targetEnt);
        entMan.AddRelation(ownerEnt, testComp.List, targetEnt);
        entMan.AddRelation(ownerEnt, testComp.Set, targetEnt);
        entMan.AddRelation(ownerEnt, testComp.DictKey, targetEnt, 1);
        entMan.AddRelation(ownerEnt, testComp.DictValue, targetEnt, testComp.DictValue.Count + 1);
    }

    private static void SetLocalRelations(EntityUid ownerEnt, EntityRelationsTestComponent testComp, EntityUid targetEnt, IEntityManager entMan)
    {
        entMan.SetLocalRelation(ownerEnt, ref testComp.LocalValue, targetEnt);
        entMan.SetLocalRelation(ownerEnt, ref testComp.LocalNullableValue, targetEnt);
        entMan.AddLocalRelation(ownerEnt, testComp.LocalList, targetEnt);
        entMan.AddLocalRelation(ownerEnt, testComp.LocalSet, targetEnt);
        entMan.AddLocalRelation(ownerEnt, testComp.LocalDictKey, targetEnt, 1);
        entMan.AddLocalRelation(ownerEnt, testComp.LocalDictValue, targetEnt, testComp.LocalDictValue.Count + 1);
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

    private static void AssertLocalTestCompTarget(EntityRelationsTestComponent testComp, EntityUid targetEnt)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(testComp.LocalValue.Entity, Is.EqualTo(targetEnt));
            Assert.That(testComp.LocalNullableValue?.Entity, Is.EqualTo(targetEnt));
            Assert.That(testComp.LocalList, Has.Count.EqualTo(1));
            Assert.That(testComp.LocalSet, Has.Count.EqualTo(1));
            Assert.That(testComp.LocalList, Does.Contain(new EntityRelation(targetEnt)));
            Assert.That(testComp.LocalSet, Does.Contain(new EntityRelation(targetEnt)));
            Assert.That(testComp.LocalDictKey, Has.Count.EqualTo(1));
            Assert.That(testComp.LocalDictValue, Has.Count.EqualTo(1));
            Assert.That(testComp.LocalDictKey, Does.ContainKey(new EntityRelation(targetEnt)));
            Assert.That(testComp.LocalDictValue, Does.ContainValue(new EntityRelation(targetEnt)));
        }
    }

    private static void AssertLocalTestCompEmpty(EntityRelationsTestComponent testComp)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(testComp.LocalValue.Entity, Is.Null);
            Assert.That(testComp.LocalNullableValue, Is.Null);
            Assert.That(testComp.LocalList, Is.Empty);
            Assert.That(testComp.LocalSet, Is.Empty);
            Assert.That(testComp.LocalDictKey, Is.Empty);
            Assert.That(testComp.LocalDictValue,
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

        [DataField]
        public EntityRelation LocalValue;

        [DataField]
        public EntityRelation? LocalNullableValue;

        [DataField]
        public List<EntityRelation> LocalList = new();

        [DataField]
        public HashSet<EntityRelation> LocalSet = new();

        [DataField]
        public Dictionary<EntityRelation, int> LocalDictKey = new();

        [DataField]
        public Dictionary<int, EntityRelation> LocalDictValue = new();
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
            SubscribeLocalEvent<EntityRelationsTestComponent, ComponentRemove>(OnRelationRemove);
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

            if (ent.Comp.LocalValue == args.Relation)
                ent.Comp.LocalValue = EntityRelation.Null;
            if (ent.Comp.LocalNullableValue.HasValue && ent.Comp.LocalNullableValue.Value == args.Relation)
                ent.Comp.LocalNullableValue = null;
            ent.Comp.LocalList.Remove(args.Relation);
            ent.Comp.LocalSet.Remove(args.Relation);
            ent.Comp.LocalDictKey.Remove(args.Relation);
            foreach (var (key, value) in ent.Comp.LocalDictValue)
            {
                if (value == args.Relation)
                    ent.Comp.LocalDictValue[key] = EntityRelation.Null;
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

            ent.Comp.LocalValue = EntityRelation.Null;
            ent.Comp.LocalNullableValue = null;
            ent.Comp.LocalList.Clear();
            ent.Comp.LocalSet.Clear();
            ent.Comp.LocalDictKey.Clear();
            foreach (var key in ent.Comp.LocalDictValue.Keys)
            {
                ent.Comp.LocalDictValue[key] = EntityRelation.Null;
            }
        }

        private void OnRelationRemove(Entity<EntityRelationsTestComponent> ent, ref ComponentRemove args)
        {
            ClearRelation(ent.Owner, ref ent.Comp.Value);
            ClearRelation(ent.Owner, ref ent.Comp.NullableValue);
            ClearRelations(ent.Owner, ent.Comp.List);
            ClearRelations(ent.Owner, ent.Comp.Set);
            ClearRelations(ent.Owner, ent.Comp.DictKey);
            ClearRelations(ent.Owner, ent.Comp.DictValue);
            ClearRelation(ent.Owner, ref ent.Comp.LocalValue);
            ClearRelation(ent.Owner, ref ent.Comp.LocalNullableValue);
            ClearRelations(ent.Owner, ent.Comp.LocalList);
            ClearRelations(ent.Owner, ent.Comp.LocalSet);
            ClearRelations(ent.Owner, ent.Comp.LocalDictKey);
            ClearRelations(ent.Owner, ent.Comp.LocalDictValue);
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
