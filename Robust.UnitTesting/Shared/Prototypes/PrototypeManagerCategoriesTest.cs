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
        var @default = _protoMan.Index<EntityPrototype>("Default");
        Assert.That(@default.Categories, Is.Empty);
        Assert.That(@default.CategoriesInternal, Is.Null);
        Assert.That(@default.HideSpawnMenu, Is.False);

        var hide = _protoMan.Index<EntityPrototype>("Hide");
        Assert.That(hide.Categories.Count, Is.EqualTo(1));
        Assert.That(hide.CategoriesInternal?.Count, Is.EqualTo(1));
        Assert.That(hide.HideSpawnMenu, Is.True);
    }

    [Test]
    public void TestInheritance()
    {
        var child = _protoMan.Index<EntityPrototype>("InheritChild");
        Assert.That(child.Categories.Count, Is.EqualTo(1));
        Assert.That(child.CategoriesInternal, Is.Null);
        Assert.That(child.HideSpawnMenu, Is.True);

        var noInheritParent = _protoMan.Index<EntityPrototype>("NoInheritParent");
        Assert.That(noInheritParent.Categories.Count, Is.EqualTo(1));
        Assert.That(noInheritParent.CategoriesInternal?.Count, Is.EqualTo(1));

        var noInheritChild = _protoMan.Index<EntityPrototype>("NoInheritChild");
        Assert.That(noInheritChild.Categories, Is.Empty);
        Assert.That(noInheritChild.CategoriesInternal, Is.Null);
    }

    [Test]
    public void TestAbstractInheritance()
    {
        Assert.That(_protoMan.HasIndex<EntityPrototype>("AbstractParent"), Is.False);
        Assert.That(_protoMan.HasIndex<EntityPrototype>("AbstractGrandChild"), Is.False);

        var concreteChild = _protoMan.Index<EntityPrototype>("ConcreteChild");
        Assert.That(concreteChild.Categories.Select(x => x.ID),
            Is.EquivalentTo(new []{"Default"}));
        Assert.That(concreteChild.CategoriesInternal, Is.Null);

        var composition = _protoMan.Index<EntityPrototype>("CompositionAbstract");
        Assert.That(composition.Categories.Select(x => x.ID),
            Is.EquivalentTo(new []{"Default", "Hide", "Auto"}));
        Assert.That(composition.CategoriesInternal, Is.Null);
    }

    [Test]
    public void TestComposition()
    {
        var compA = _protoMan.Index<EntityPrototype>("CompositionA");
        Assert.That(compA.Categories.Select(x => x.ID),
            Is.EquivalentTo(new []{"Default", "Hide"}));
        Assert.That(compA.CategoriesInternal?.Count, Is.EqualTo(2));

        var compB = _protoMan.Index<EntityPrototype>("CompositionB");
        Assert.That(compB.Categories.Select(x => x.ID),
            Is.EquivalentTo(new []{"Default", "NoInherit", "Auto"}));
        Assert.That(compB.CategoriesInternal?.Count, Is.EqualTo(3));

        var childA = _protoMan.Index<EntityPrototype>("CompositionChildA");
        Assert.That(childA.Categories.Select(x => x.ID),
            Is.EquivalentTo(new []{"Default", "Hide", "Auto"}));
        Assert.That(childA.CategoriesInternal, Is.Null);

        var childB = _protoMan.Index<EntityPrototype>("CompositionChildB");
        Assert.That(childB.Categories.Select(x => x.ID),
            Is.EquivalentTo(new []{"Default", "Hide", "Auto", "Default2"}));
        Assert.That(childB.CategoriesInternal?.Count, Is.EqualTo(1));
    }

    [Test]
    public void TestAutoCategorization()
    {
        var auto = _protoMan.Index<EntityPrototype>("Auto");
        Assert.That(auto.Categories.Select(x => x.ID), Is.EquivalentTo(new []{"Auto"}));
        Assert.That(auto.CategoriesInternal, Is.Null);

        //var autoAttrib = _protoMan.Index<EntityPrototype>("AutoAttribute");
        //Assert.That(autoAttrib.Categories.Select(x => x.ID), Is.EquivalentTo(new []{"Auto"}));
        //Assert.That(autoAttrib.CategoriesInternal, Is.Null);

        var autoChild = _protoMan.Index<EntityPrototype>("AutoChild");
        Assert.That(autoChild.Categories.Select(x => x.ID),
            Is.EquivalentTo(new []{"Auto", "Default"}));
        Assert.That(autoChild.CategoriesInternal?.Count, Is.EqualTo(1));


    }

    [Test]
    public void TestCategoryGrouping()
    {
        var none = _protoMan.Categories[new("None")].Select(x=> x.ID);
        Assert.That(none, Is.Empty);

        var @default = _protoMan.Categories[new("Default")].Select(x=> x.ID);
        Assert.That(@default, Is.EquivalentTo(new[] {"ConcreteChild", "CompositionAbstract", "CompositionA", "CompositionB", "CompositionChildA", "CompositionChildB", "AutoChild"}));

        var default2 = _protoMan.Categories[new("Default2")].Select(x=> x.ID);
        Assert.That(default2, Is.EquivalentTo(new[] {"CompositionChildB"}));

        var hide = _protoMan.Categories[new("Hide")].Select(x=> x.ID);
        Assert.That(hide, Is.EquivalentTo(new[] {"Hide", "CompositionAbstract", "CompositionA", "CompositionChildA", "CompositionChildB", "InheritChild"}));

        var noInherit = _protoMan.Categories[new("NoInherit")].Select(x=> x.ID);
        Assert.That(noInherit, Is.EquivalentTo(new[] {"NoInheritParent", "CompositionB"}));

        var auto = _protoMan.Categories[new("Auto")].Select(x=> x.ID);
        Assert.That(auto, Is.EquivalentTo(new[] {"CompositionAbstract", "CompositionB", "CompositionChildA", "CompositionChildB", "Auto", "AutoChild"}));//, "AutoAttribute"}));
    }

    const string TestPrototypes = @"
- type: entityCategory
  id: None

- type: entityCategory
  id: Default

- type: entityCategory
  id: Default2

- type: entityCategory
  id: Hide
  hideSpawnMenu: true

- type: entityCategory
  id: NoInherit
  inheritable: false

- type: entityCategory
  id: Auto
  components: [ AutoCategory ]

- type: entity
  id: Default

- type: entity
  id: Hide
  categories: [ Hide ]

- type: entity
  id: InheritChild
  parent: Hide

- type: entity
  id: NoInheritParent
  categories: [ NoInherit ]

- type: entity
  id: NoInheritChild
  parent: NoInheritParent

- type: entity
  id: CompositionA
  categories: [ Default, Hide ]

- type: entity
  id: CompositionB
  categories: [ Default, NoInherit, Auto ]

- type: entity
  id: CompositionChildA
  parent: [CompositionA, CompositionB]

- type: entity
  id: CompositionChildB
  parent: [CompositionA, CompositionB]
  categories: [ Default2 ]

- type: entity
  id: AbstractParent
  abstract: true
  categories: [ Default ]

- type: entity
  id: ConcreteChild
  parent: AbstractParent

- type: entity
  abstract: true
  id: AbstractGrandChild
  parent: ConcreteChild
  categories: [ Hide ]

- type: entity
  id: CompositionAbstract
  parent: [ AbstractGrandChild, CompositionB ]

- type: entity
  id: Auto
  components:
  - type: AutoCategory

#- type: entity
#  id: AutoAttribute
#  components:
#  - type: AttributeAutoCategory

- type: entity
  id: AutoParent
  abstract: true
  categories: [ NoInherit ]
  components:
  - type: AutoCategory

- type: entity
  id: AutoChild
  parent: AutoParent
  categories: [ Default ]
";
}

public sealed partial class AutoCategoryComponent : Component;

// TODO test-local IReflectionManager
// [EntityCategory("Auto")]
// public sealed partial class AttributeAutoCategoryComponent : Component;
