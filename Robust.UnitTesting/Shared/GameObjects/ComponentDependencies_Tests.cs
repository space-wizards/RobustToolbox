using System.IO;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Robust.UnitTesting.Shared.GameObjects
{
    [TestFixture]
    public class ComponentDependenciesTests : RobustUnitTest
    {
        private const string Prototypes = @"
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
  - type: TestInterface
  - type: TestFour

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
  - type: TestThree

- type: entity
  name: dummy
  id: dummyFour
  components:
  - type: TestInterface
  - type: TestFour

- type: entity
  name: dummy
  id: dummyFive
  components:
  - type: TestFive

- type: entity
  name: dummy
  id: dummySix
  components:
  - type: TestSix
";

        private class TestOneComponent : Component
        {
            public override string Name => "TestOne";

            [ComponentDependency(nameof(TestTwoAdded), nameof(TestTwoRemoved))]
            public readonly TestTwoComponent? TestTwo = default!;

            [ComponentDependency]
            public readonly TestThreeComponent? TestThree = default!;

            [ComponentDependency] public readonly TestFourComponent? TestFour = default!;

            public bool TestTwoIsAdded { get; private set; }

            private void TestTwoAdded()
            {
                TestTwoIsAdded = true;
            }

            private void TestTwoRemoved()
            {
                TestTwoIsAdded = false;
            }
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

        private interface ITestInterfaceInterface : IComponent { }

        private interface ITestInterfaceUnreferenced : IComponent { }

        private class TestInterfaceComponent : Component, ITestInterfaceInterface, ITestInterfaceUnreferenced
        {
            public override string Name => "TestInterface";
        }

        private class TestFourComponent : Component
        {
            public override string Name => "TestFour";

            [ComponentDependency] public readonly ITestInterfaceInterface? TestInterface = default!;

            [ComponentDependency] public readonly ITestInterfaceUnreferenced? TestInterfaceUnreferenced = default!;
        }

        private class TestFiveComponent : Component
        {
            public override string Name => "TestFive";

#pragma warning disable 649
            [ComponentDependency] public bool? Thing;
#pragma warning restore 649
        }

        private class TestSixComponent : Component
        {
            public override string Name => "TestSix";

            [ComponentDependency] public TestFiveComponent Thing = null!;
        }

        private class TestSevenComponent : Component
        {
            public override string Name => "TestSeven";

            [ComponentDependency("ABCDEF")] public TestFiveComponent? Thing = null!;
        }

        [OneTimeSetUp]
        public void Setup()
        {
            var componentFactory = IoCManager.Resolve<IComponentFactory>();
            componentFactory.Register<TestOneComponent>();
            componentFactory.Register<TestTwoComponent>();
            componentFactory.Register<TestThreeComponent>();
            componentFactory.Register<TestInterfaceComponent>();
            componentFactory.RegisterReference<TestInterfaceComponent, ITestInterfaceInterface>();
            componentFactory.Register<TestFourComponent>();
            componentFactory.Register<TestFiveComponent>();
            componentFactory.Register<TestSixComponent>();
            componentFactory.Register<TestSevenComponent>();

            IoCManager.Resolve<ISerializationManager>().Initialize();
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.LoadFromStream(new StringReader(Prototypes));
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

            // Dummy with TestInterface and TestFour.
            var dummyFour = entityManager.CreateEntityUninitialized("dummyFour");

            Assert.That(dummyFour, Is.Not.Null);

            var dummyFourComp = dummyFour.GetComponent<TestFourComponent>();

            // This dependency should be resolved.
            Assert.That(dummyFourComp.TestInterface, Is.Not.Null);
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

            // TestFour should not be resolved.
            Assert.That(dummyOneComp.TestFour, Is.Null);

            dummyThree.AddComponent<TestFourComponent>();

            // TestFour should now be resolved
            Assert.That(dummyOneComp.TestFour, Is.Not.Null);

            var dummyFourComp = dummyOneComp.TestFour;

            dummyThree.AddComponent<TestInterfaceComponent>();

            // This dependency should now be resolved.
            Assert.That(dummyFourComp!.TestInterface, Is.Not.Null);
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

            // It should have TestFour and TestInterface.
            Assert.That(dummyComp.TestFour, Is.Not.Null);
            Assert.That(dummyComp.TestFour!.TestInterface, Is.Not.Null);

            // Remove the interface.
            dummyOne.RemoveComponent<TestInterfaceComponent>();

            // TestInterface should now be null, but TestFour should not be.
            Assert.That(dummyComp.TestFour, Is.Not.Null);
            Assert.That(dummyComp.TestFour.TestInterface, Is.Null);

            // Remove TestFour.
            dummyOne.RemoveComponent<TestFourComponent>();

            // TestFour should now be null.
            Assert.That(dummyComp.TestFour, Is.Null);
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

            // Add test four.
            dummy.AddComponent<TestFourComponent>();

            // TestFour should not be null, but TestInterface should be.
            Assert.That(testOne.TestFour, Is.Not.Null);
            Assert.That(testOne.TestFour!.TestInterface, Is.Null);

            // Remove test four
            dummy.RemoveComponent<TestFourComponent>();

            // Now the dependency is null.
            Assert.That(testOne.TestFour, Is.Null);
        }

        [Test]
        public void NoUnreferencedInterfaceTest()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();

            // An entity with TestFour.
            var dummyFour = entityManager.CreateEntityUninitialized("dummyFour");

            Assert.That(dummyFour, Is.Not.Null);

            var dummyComp = dummyFour.GetComponent<TestFourComponent>();

            // TestInterface must be resolved.
            Assert.That(dummyComp.TestInterface, Is.Not.Null);

            // TestInterfaceUnreferenced should not be.
            Assert.That(dummyComp.TestInterfaceUnreferenced, Is.Null);
        }

        [Test]
        public void RemoveInterfaceDependencyTest()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();

            // An entity with TestFour.
            var dummyFour = entityManager.CreateEntityUninitialized("dummyFour");

            Assert.That(dummyFour, Is.Not.Null);

            var dummyComp = dummyFour.GetComponent<TestFourComponent>();

            // TestInterface must be resolved.
            Assert.That(dummyComp.TestInterface, Is.Not.Null);

            // Remove TestInterface through its referenced interface.
            dummyFour.RemoveComponent<ITestInterfaceInterface>();

            // TestInterface must be null.
            Assert.That(dummyComp.TestInterface, Is.Null);
        }

        [Test]
        public void ValueTypeFieldTest()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();

            // An entity with TestFive.
            var except = Assert.Throws<EntityCreationException>(() => entityManager.CreateEntityUninitialized("dummyFive"));
            Assert.That(except, Is.Not.Null);
            Assert.That(except!.InnerException, Is.TypeOf<ComponentDependencyValueTypeException>());
        }

        [Test]
        public void NotNullableFieldTest()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();

            // An entity with TestSix.
            var except = Assert.Throws<EntityCreationException>(() => entityManager.CreateEntityUninitialized("dummySix"));
            Assert.That(except, Is.Not.Null);
            Assert.That(except!.InnerException, Is.TypeOf<ComponentDependencyNotNullableException>());
        }

        [Test]
        public void OnAddRemoveMethodTest()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var entity = entityManager.CreateEntityUninitialized("dummy");
            var t1Comp = entity.AddComponent<TestOneComponent>();

            Assert.That(t1Comp.TestTwoIsAdded, Is.False);

            entity.AddComponent<TestTwoComponent>();

            Assert.That(t1Comp.TestTwoIsAdded, Is.True);

            entity.RemoveComponent<TestTwoComponent>();

            Assert.That(t1Comp.TestTwoIsAdded, Is.False);
        }

        [Test]
        public void OnAddRemoveMethodInvalidTest()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var entity = entityManager.CreateEntityUninitialized("dummy");
            try
            {
                var t7Comp = entity.AddComponent<TestSevenComponent>();
            }
            catch (ComponentDependencyInvalidMethodNameException invEx)
            {
                Assert.That(invEx, Is.Not.Null);
                return;
            }

            Assert.Fail("No exception thrown");
        }
    }
}
