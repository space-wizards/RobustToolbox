using System.IO;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.ComponentDependencies;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Robust.UnitTesting.Shared.GameObjects
{
    [TestFixture]
    public class ComponentDependencies_Tests : RobustUnitTest
    {
        private const string PROTOTYPES = @"
- type: entity
  name: dummy
  id: dummy
  components:
  - type: Transform

- type: entity
  name: dummy
  id: dummyOne
  components:
  - type: Transform
  - type: TestOne
  - type: TestTwo
  - type: TestThree

- type: entity
  name: dummy
  id: dummyTwo
  components:
  - type: Transform
  - type: TestTwo

- type: entity
  name: dummy
  id: dummyThree
  components:
  - type: Transform
  - type: TestThree";

        private class TestOneComponent : Component
        {
            public override string Name => "TestOne";

            [ComponentDependency]
            public readonly TestTwoComponent? TestTwo = default!;

            [ComponentDependency]
            public readonly TestThreeComponent? TestThree = default!;
        }

        private class TestTwoComponent : Component
        {
            public override string Name => "TestTwo";

            // This silly component wants itself!
            [ComponentDependency]
            public readonly TestTwoComponent? TestTwo = default!;

            [ComponentDependency]
            public readonly TransformComponent? Transform = default!;
        }

        private class TestThreeComponent : Component
        {
            public override string Name => "TestThree";

            [ComponentDependency]
            public readonly TestOneComponent? TestOne = default!;
        }

        [OneTimeSetUp]
        public void Setup()
        {
            var componentFactory = IoCManager.Resolve<IComponentFactory>();
            componentFactory.Register<TestOneComponent>();
            componentFactory.Register<TestTwoComponent>();
            componentFactory.Register<TestThreeComponent>();

            var componentManager = IoCManager.Resolve<IComponentManager>();
            componentManager.Initialize();

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.LoadFromStream(new StringReader(PROTOTYPES));
            prototypeManager.Resync();
        }

        [Test]
        public void ComponentDependenciesResolvedPrototypeTest()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();

            // This dummy should have all of its dependencies resolved.
            var dummyOne = entityManager.CreateEntityUninitialized("dummyOne");

            Assert.That(dummyOne, Is.Not.Null);

            var dummyComp = dummyOne.GetComponent<TestOneComponent>();

            Assert.That(dummyComp.TestTwo, Is.Not.Null);
            Assert.That(dummyComp.TestThree, Is.Not.Null);

            // Test two's dependency on itself shouldn't be null, it should be itself.
            Assert.That(dummyComp.TestTwo!.TestTwo, Is.Not.Null);
            Assert.That(dummyComp.TestTwo!.TestTwo, Is.EqualTo(dummyComp.TestTwo));

            // Test two's dependency on Transform should be correct.
            Assert.That(dummyComp.TestTwo!.Transform, Is.Not.Null);
            Assert.That(dummyComp.TestTwo!.Transform, Is.EqualTo(dummyOne.Transform));

            // Test three's dependency on test one should be correct.
            Assert.That(dummyComp.TestThree!.TestOne, Is.Not.Null);
            Assert.That(dummyComp.TestThree!.TestOne, Is.EqualTo(dummyComp));

            // Dummy with only TestTwo.
            var dummyTwo = entityManager.CreateEntityUninitialized("dummyTwo");

            Assert.That(dummyTwo, Is.Not.Null);

            var dummyTwoComp = dummyTwo.GetComponent<TestTwoComponent>();

            // This dependency should be resolved.
            Assert.That(dummyTwoComp.TestTwo, Is.Not.Null);
            Assert.That(dummyTwoComp.Transform, Is.Not.Null);

            // Dummy with only TestThree.
            var dummyThree = entityManager.CreateEntityUninitialized("dummyThree");

            Assert.That(dummyThree, Is.Not.Null);

            var dummyThreeComp = dummyThree.GetComponent<TestThreeComponent>();

            // This dependency should be unresolved.
            Assert.That(dummyThreeComp.TestOne, Is.Null);
        }

        [Test]
        public void AddComponentDependencyTest()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();

            // Dummy with only TestThree.
            var dummyThree = entityManager.CreateEntityUninitialized("dummyThree");

            Assert.That(dummyThree, Is.Not.Null);

            var dummyThreeComp = dummyThree.GetComponent<TestThreeComponent>();

            // This dependency should be unresolved at first.
            Assert.That(dummyThreeComp.TestOne, Is.Null);

            // We add the TestOne component...
            dummyThree.AddComponent<TestOneComponent>();

            // This dependency should be resolved now!
            Assert.That(dummyThreeComp.TestOne, Is.Not.Null);

            var dummyOneComp = dummyThreeComp.TestOne;

            // This dependency should be resolved.
            Assert.That(dummyOneComp!.TestThree, Is.Not.Null);

            // This dependency should still be unresolved.
            Assert.That(dummyOneComp.TestTwo, Is.Null);

            dummyThree.AddComponent<TestTwoComponent>();

            // And now it is resolved!
            Assert.That(dummyOneComp.TestTwo, Is.Not.Null);
        }

        [Test]
        public void RemoveComponentDependencyTest()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();

            // This dummy should have all of its dependencies resolved.
            var dummyOne = entityManager.CreateEntityUninitialized("dummyOne");

            Assert.That(dummyOne, Is.Not.Null);

            var dummyComp = dummyOne.GetComponent<TestOneComponent>();

            // They must be resolved.
            Assert.That(dummyComp.TestTwo, Is.Not.Null);
            Assert.That(dummyComp.TestThree, Is.Not.Null);

            // And now, we remove TestTwo.
            dummyOne.RemoveComponent<TestTwoComponent>();

            // It has become null!
            Assert.That(dummyComp.TestTwo, Is.Null);

            // Test three should still be there...
            Assert.That(dummyComp.TestThree, Is.Not.Null);

            // But not for long.
            dummyOne.RemoveComponent<TestThreeComponent>();

            // It should now be null!
            Assert.That(dummyComp.TestThree, Is.Null);
        }

        [Test]
        public void AddAndRemoveComponentDependencyTest()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();

            // An entity with no components.
            var dummy = entityManager.CreateEntityUninitialized("dummy");

            // First we add test one.
            var testOne = dummy.AddComponent<TestOneComponent>();

            // We check the dependencies are null.
            Assert.That(testOne.TestTwo, Is.Null);
            Assert.That(testOne.TestThree, Is.Null);

            // We add test two.
            var testTwo = dummy.AddComponent<TestTwoComponent>();

            // Check that everything is in order.
            Assert.That(testOne.TestTwo, Is.Not.Null);
            Assert.That(testOne.TestTwo, Is.EqualTo(testTwo));

            // Remove test two...
            testTwo = null;
            dummy.RemoveComponent<TestTwoComponent>();

            // The dependency should be null now.
            Assert.That(testOne.TestTwo, Is.Null);

            // We add test three.
            var testThree = dummy.AddComponent<TestThreeComponent>();

            // All should be in order again.
            Assert.That(testOne.TestThree, Is.Not.Null);
            Assert.That(testOne.TestThree, Is.EqualTo(testThree));

            Assert.That(testThree.TestOne, Is.Not.Null);
            Assert.That(testThree.TestOne, Is.EqualTo(testOne));

            // Remove test one.
            testOne = null;
            dummy.RemoveComponent<TestOneComponent>();

            // Now the dependency is null.
            Assert.That(testThree.TestOne, Is.Null);

            // Let's actually remove the removed components first.
            dummy.EntityManager.ComponentManager.CullRemovedComponents();

            // Re-add test one and two.
            testOne = dummy.AddComponent<TestOneComponent>();
            testTwo = dummy.AddComponent<TestTwoComponent>();

            // All should be fine again!
            Assert.That(testThree.TestOne, Is.Not.Null);
            Assert.That(testThree.TestOne, Is.EqualTo(testOne));

            Assert.That(testOne.TestThree, Is.Not.Null);
            Assert.That(testOne.TestThree, Is.EqualTo(testThree));

            Assert.That(testTwo.TestTwo, Is.Not.Null);
            Assert.That(testTwo.TestTwo, Is.EqualTo(testTwo));
        }
    }
}
