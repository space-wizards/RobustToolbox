using System;
using System.Linq;
using JetBrains.Annotations;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Robust.UnitTesting.Shared.Prototypes;

[UsedImplicitly]
[TestFixture]
public sealed class PrototypeManagerCategoriesTest : RobustUnitTest
{

    private IPrototypeManager _protoMan = default!;
    protected override Type[] ExtraComponents => [typeof(AutoCategoryComponent)];//, typeof(AttributeAutoCategoryComponent)];

    [OneTimeSetUp]
    public void Setup()
    {
        IoCManager.Resolve<ISerializationManager>().Initialize();
        _protoMan = IoCManager.Resolve<IPrototypeManager>();
        _protoMan.RegisterKind(typeof(EntityPrototype), typeof(EntityCategoryPrototype));
        _protoMan.LoadString(TestPrototypes);
        _protoMan.ResolveResults();
    }

    [Test]
    public void TestExplicitCategories()
    {
        var @default = _protoMan.Index<EntityPrototype>(DefaultEntity);
        Assert.That(@default.Categories, Is.Empty);
        Assert.That(@default.CategoriesInternal, Is.Null);
        Assert.That(@default.HideSpawnMenu, Is.False);

        var hide = _protoMan.Index<EntityPrototype>(HideEntity);
        Assert.That(hide.Categories.Count, Is.EqualTo(1));
        Assert.That(hide.CategoriesInternal?.Count, Is.EqualTo(1));
        Assert.That(hide.HideSpawnMenu, Is.True);
    }

    [Test]
    public void TestInheritance()
    {
        var child = _protoMan.Index<EntityPrototype>(InheritChildEntity);
        Assert.That(child.Categories.Count, Is.EqualTo(1));
        Assert.That(child.CategoriesInternal, Is.Null);
        Assert.That(child.HideSpawnMenu, Is.True);

        var noInheritParent = _protoMan.Index<EntityPrototype>(NoInheritParentEntity);
        Assert.That(noInheritParent.Categories.Count, Is.EqualTo(1));
        Assert.That(noInheritParent.CategoriesInternal?.Count, Is.EqualTo(1));

        var noInheritChild = _protoMan.Index<EntityPrototype>(NoInheritChildEntity);
        Assert.That(noInheritChild.Categories, Is.Empty);
        Assert.That(noInheritChild.CategoriesInternal, Is.Null);
    }

    [Test]
    public void TestAbstractInheritance()
    {
        Assert.That(_protoMan.HasIndex<EntityPrototype>(AbstractParentEntity), Is.False);
        Assert.That(_protoMan.HasIndex<EntityPrototype>(AbstractGrandChildEntity), Is.False);

        var concreteChild = _protoMan.Index<EntityPrototype>(ConcreteChildEntity);
        Assert.That(concreteChild.Categories.Select(x => x.ID),
            Is.EquivalentTo(new []{DefaultCategory}));
        Assert.That(concreteChild.CategoriesInternal, Is.Null);

        var composition = _protoMan.Index<EntityPrototype>(CompositionAbstractEntity);
        Assert.That(composition.Categories.Select(x => x.ID),
            Is.EquivalentTo(new []{DefaultCategory, HideCategory, AutoCategory}));
        Assert.That(composition.CategoriesInternal, Is.Null);
    }

    [Test]
    public void TestComposition()
    {
        var compA = _protoMan.Index<EntityPrototype>(CompositionAEntity);
        Assert.That(compA.Categories.Select(x => x.ID),
            Is.EquivalentTo(new []{DefaultCategory, HideCategory}));
        Assert.That(compA.CategoriesInternal?.Count, Is.EqualTo(2));

        var compB = _protoMan.Index<EntityPrototype>(CompositionBEntity);
        Assert.That(compB.Categories.Select(x => x.ID),
            Is.EquivalentTo(new []{DefaultCategory, NoInheritCategory, AutoCategory}));
        Assert.That(compB.CategoriesInternal?.Count, Is.EqualTo(3));

        var childA = _protoMan.Index<EntityPrototype>(CompositionChildAEntity);
        Assert.That(childA.Categories.Select(x => x.ID),
            Is.EquivalentTo(new []{DefaultCategory, HideCategory, AutoCategory}));
        Assert.That(childA.CategoriesInternal, Is.Null);

        var childB = _protoMan.Index<EntityPrototype>(CompositionChildBEntity);
        Assert.That(childB.Categories.Select(x => x.ID),
            Is.EquivalentTo(new []{DefaultCategory, HideCategory, AutoCategory, Default2Category}));
        Assert.That(childB.CategoriesInternal?.Count, Is.EqualTo(1));
    }

    [Test]
    public void TestAutoCategorization()
    {
        var auto = _protoMan.Index<EntityPrototype>(AutoEntity);
        Assert.That(auto.Categories.Select(x => x.ID), Is.EquivalentTo(new []{AutoCategory}));
        Assert.That(auto.CategoriesInternal, Is.Null);

        //var autoAttrib = _protoMan.Index<EntityPrototype>("AutoAttribute");
        //Assert.That(autoAttrib.Categories.Select(x => x.ID), Is.EquivalentTo(new []{"Auto"}));
        //Assert.That(autoAttrib.CategoriesInternal, Is.Null);

        var autoChild = _protoMan.Index<EntityPrototype>(AutoChildEntity);
        Assert.That(autoChild.Categories.Select(x => x.ID),
            Is.EquivalentTo(new []{AutoCategory, DefaultCategory}));
        Assert.That(autoChild.CategoriesInternal?.Count, Is.EqualTo(1));


    }

    [Test]
    public void TestCategoryGrouping()
    {
        var none = _protoMan.Categories[new(NoneCategory)].Select(x=> x.ID);
        Assert.That(none, Is.Empty);

        var @default = _protoMan.Categories[new(DefaultCategory)].Select(x=> x.ID);
        Assert.That(@default, Is.EquivalentTo(new[] {ConcreteChildEntity, CompositionAbstractEntity, CompositionAEntity, CompositionBEntity, CompositionChildAEntity, CompositionChildBEntity, AutoChildEntity}));

        var default2 = _protoMan.Categories[new(Default2Category)].Select(x=> x.ID);
        Assert.That(default2, Is.EquivalentTo(new[] {CompositionChildBEntity}));

        var hide = _protoMan.Categories[new(HideCategory)].Select(x=> x.ID);
        Assert.That(hide, Is.EquivalentTo(new[] {HideEntity, CompositionAbstractEntity, CompositionAEntity, CompositionChildAEntity, CompositionChildBEntity, InheritChildEntity}));

        var noInherit = _protoMan.Categories[new(NoInheritCategory)].Select(x=> x.ID);
        Assert.That(noInherit, Is.EquivalentTo(new[] {NoInheritParentEntity, CompositionBEntity}));

        var auto = _protoMan.Categories[new(AutoCategory)].Select(x=> x.ID);
        Assert.That(auto, Is.EquivalentTo(new[] {CompositionAbstractEntity, CompositionBEntity, CompositionChildAEntity, CompositionChildBEntity, AutoEntity, AutoChildEntity}));//, "AutoAttribute"}));
    }

    const string NoneCategory = "None";
    const string DefaultCategory = "Default";
    const string Default2Category = "Default2";
    const string HideCategory = "Hide";
    const string NoInheritCategory = "NoInherit";
    const string AutoCategory = "Auto";

    const string DefaultEntity = "Default";
    const string HideEntity = "Hide";
    const string InheritChildEntity = "InheritChild";
    const string NoInheritParentEntity = "NoInheritParent";
    const string NoInheritChildEntity = "NoInheritChild";
    const string CompositionAEntity = "CompositionA";
    const string CompositionBEntity = "CompositionB";
    const string CompositionChildAEntity = "CompositionChildA";
    const string CompositionChildBEntity = "CompositionChildB";
    const string AbstractParentEntity = "AbstractParent";
    const string ConcreteChildEntity = "ConcreteChild";
    const string AbstractGrandChildEntity = "AbstractGrandChild";
    const string CompositionAbstractEntity = "CompositionAbstract";
    const string AutoEntity = "Auto";
    const string AutoParentEntity = "AutoParent";
    const string AutoChildEntity = "AutoChild";

    const string TestPrototypes = $@"
- type: entityCategory
  id: {NoneCategory}

- type: entityCategory
  id: {DefaultCategory}

- type: entityCategory
  id: {Default2Category}

- type: entityCategory
  id: {HideCategory}
  hideSpawnMenu: true

- type: entityCategory
  id: {NoInheritCategory}
  inheritable: false

- type: entityCategory
  id: {AutoCategory}
  components: [ AutoCategory ]

- type: entity
  id: {DefaultEntity}

- type: entity
  id: {HideEntity}
  categories: [ {HideCategory} ]

- type: entity
  id: {InheritChildEntity}
  parent: Hide

- type: entity
  id: {NoInheritParentEntity}
  categories: [ {NoInheritCategory} ]

- type: entity
  id: {NoInheritChildEntity}
  parent: NoInheritParent

- type: entity
  id: {CompositionAEntity}
  categories: [ {DefaultCategory}, {HideCategory} ]

- type: entity
  id: {CompositionBEntity}
  categories: [ {DefaultCategory}, {NoInheritCategory}, {AutoCategory} ]

- type: entity
  id: {CompositionChildAEntity}
  parent: [{CompositionAEntity}, {CompositionBEntity}]

- type: entity
  id: {CompositionChildBEntity}
  parent: [{CompositionAEntity}, {CompositionBEntity}]
  categories: [ {Default2Category} ]

- type: entity
  id: {AbstractParentEntity}
  abstract: true
  categories: [ {DefaultCategory} ]

- type: entity
  id: {ConcreteChildEntity}
  parent: {AbstractParentEntity}

- type: entity
  abstract: true
  id: {AbstractGrandChildEntity}
  parent: {ConcreteChildEntity}
  categories: [ {HideCategory} ]

- type: entity
  id: {CompositionAbstractEntity}
  parent: [ {AbstractGrandChildEntity}, {CompositionBEntity} ]

- type: entity
  id: {AutoEntity}
  components:
  - type: AutoCategory

#- type: entity
#  id: AutoAttribute
#  components:
#  - type: AttributeAutoCategory

- type: entity
  id: {AutoParentEntity}
  abstract: true
  categories: [ {NoInheritCategory} ]
  components:
  - type: AutoCategory

- type: entity
  id: {AutoChildEntity}
  parent: {AutoParentEntity}
  categories: [ {DefaultCategory} ]
";
}

public sealed partial class AutoCategoryComponent : Component;

// TODO test-local IReflectionManager
// [EntityCategory("Auto")]
// public sealed partial class AttributeAutoCategoryComponent : Component;
