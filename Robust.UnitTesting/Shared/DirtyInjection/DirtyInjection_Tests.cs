using System;
using System.IO;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Injections;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Robust.UnitTesting.Shared.DirtyInjection
{
    [TestFixture]
    public class DirtyInjection_Tests : RobustUnitTest
    {
        private const string PROTOTYPE = @"
- type: entity
  name: dummy
  id: dummy
  components:
  - type: TestComp";

        [OneTimeSetUp]
        public void Setup()
        {
            var componentFactory = IoCManager.Resolve<IComponentFactory>();
            componentFactory.Register<DirtyInjectionTestComponent>();

            var componentManager = IoCManager.Resolve<IComponentManager>();
            componentManager.Initialize();

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.LoadFromStream(new StringReader(PROTOTYPE));
            prototypeManager.Resync();
        }

        [Test]
        public void DirtyInjectionStringTest()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var dummy = entityManager.CreateEntityUninitialized("dummy");
            var testComp = dummy.GetComponent<DirtyInjectionTestComponent>();
            var tick = testComp.LastModifiedTick;
            testComp.StringTest = "SOMETHING NEW";
            Assert.That(tick, NUnit.Framework.Is.Not.EqualTo(testComp.LastModifiedTick));
        }

        [Test]
        public void DirtyInjectionFloatTest()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var dummy = entityManager.CreateEntityUninitialized("dummy");
            var testComp = dummy.GetComponent<DirtyInjectionTestComponent>();
            var tick = testComp.LastModifiedTick;
            testComp.FloatTest = 1234.56f;
            Assert.That(tick, NUnit.Framework.Is.Not.EqualTo(testComp.LastModifiedTick));
        }

        [Test]
        public void DirtyInjectionIntTest()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var dummy = entityManager.CreateEntityUninitialized("dummy");
            var testComp = dummy.GetComponent<DirtyInjectionTestComponent>();
            var tick = testComp.LastModifiedTick;
            testComp.IntTest = 123456;
            Assert.That(tick, NUnit.Framework.Is.Not.EqualTo(testComp.LastModifiedTick));
        }

        [Test]
        public void DirtyInjectionCompTest()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var dummy = entityManager.CreateEntityUninitialized("dummy");
            var testComp = dummy.GetComponent<DirtyInjectionTestComponent>();
            var tick = testComp.LastModifiedTick;
            testComp.CompTest = new DirtyInjectionTestComponent();
            Assert.That(tick, NUnit.Framework.Is.Not.EqualTo(testComp.LastModifiedTick));
        }

        [Test]
        public void DirtyInjectionStructTest()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var dummy = entityManager.CreateEntityUninitialized("dummy");
            var testComp = dummy.GetComponent<DirtyInjectionTestComponent>();
            var tick = testComp.LastModifiedTick;
            testComp.StructTest = Int32.Parse("123");
            Assert.That(tick, NUnit.Framework.Is.Not.EqualTo(testComp.LastModifiedTick));
        }

        [Test]
        public void DirtyInjectionEnumTest()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var dummy = entityManager.CreateEntityUninitialized("dummy");
            var testComp = dummy.GetComponent<DirtyInjectionTestComponent>();
            var tick = testComp.LastModifiedTick;
            testComp.EnumTest = DirtyCallTestEnum.Value2;
            Assert.That(tick, NUnit.Framework.Is.Not.EqualTo(testComp.LastModifiedTick));
        }
    }

    public class DirtyInjectionTestComponent : Component
    {
        public override string Name => "TestComp";

        [Dirty] public string StringTest { get; set; }

        [Dirty] public float FloatTest { get; set; }

        [Dirty] public int IntTest { get; set; }

        [Dirty] public Component CompTest { get; set; }

        [Dirty] public Int32 StructTest { get; set; }

        [Dirty] public DirtyCallTestEnum EnumTest { get; set; } = DirtyCallTestEnum.Value1;
    }

    public enum DirtyCallTestEnum
    {
        Value1,
        Value2
    }
}
