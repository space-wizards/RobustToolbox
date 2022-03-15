using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.IoC.Exceptions;

namespace Robust.UnitTesting.Shared.GameObjects
{
    [TestFixture, TestOf(typeof(EntitySystemManager))]
    public sealed class EntitySystemManager_Tests: RobustUnitTest
    {

        public abstract class ESystemBase : IEntitySystem
        {
            public virtual IEnumerable<Type> UpdatesAfter => Enumerable.Empty<Type>();
            public virtual IEnumerable<Type> UpdatesBefore => Enumerable.Empty<Type>();
            public bool UpdatesOutsidePrediction => true;
            public void Initialize() { }
            public void Shutdown() { }
            public void Update(float frameTime) { }
            public void FrameUpdate(float frameTime) { }
        }
        [Virtual]
        public class ESystemA : ESystemBase { }
        public sealed class ESystemC : ESystemA { }
        public abstract class ESystemBase2 : ESystemBase { }
        public sealed class ESystemB : ESystemBase2 { }

        public sealed class ESystemDepA : ESystemBase
        {
            [Dependency] public readonly ESystemDepB ESystemDepB = default!;
        }

        public sealed class ESystemDepB : ESystemBase
        {
            [Dependency] public readonly ESystemDepA ESystemDepA = default!;
        }

        /*
         ESystemBase (Abstract)
           - ESystemA
             - ESystemC
           - EsystemBase2 (Abstract)
             - ESystemB

         */

        [OneTimeSetUp]
        public void Setup()
        {
            var syssy = IoCManager.Resolve<IEntitySystemManager>();
            syssy.Clear();
            syssy.LoadExtraSystemType<ESystemA>();
            syssy.LoadExtraSystemType<ESystemB>();
            syssy.LoadExtraSystemType<ESystemC>();
            syssy.LoadExtraSystemType<ESystemDepA>();
            syssy.LoadExtraSystemType<ESystemDepB>();
            syssy.Initialize(false);
        }

        [Test]
        public void GetsByTypeOrSupertype()
        {
            var esm = IoCManager.Resolve<IEntitySystemManager>();

            // getting type by the exact type should work fine
            Assert.That(esm.GetEntitySystem<ESystemB>(), Is.TypeOf<ESystemB>());

            // getting type by an abstract supertype should work fine
            // because there are no other subtypes of that supertype it would conflict with
            // it should return the only concrete subtype
            Assert.That(esm.GetEntitySystem<ESystemBase2>(), Is.TypeOf<ESystemB>());

            // getting ESystemA type by its exact type should work fine,
            // even though EsystemC is a subtype - it should return an instance of ESystemA
            var esysA = esm.GetEntitySystem<ESystemA>();
            Assert.That(esysA, Is.TypeOf<ESystemA>());
            Assert.That(esysA, Is.Not.TypeOf<ESystemC>());

            var esysC = esm.GetEntitySystem<ESystemC>();
            Assert.That(esysC, Is.TypeOf<ESystemC>());

            // this should not work - it's abstract and there are multiple
            // concrete subtypes
            Assert.Throws<UnregisteredTypeException>(() =>
            {
                esm.GetEntitySystem<ESystemBase>();
            });
        }

        [Test]
        public void DependencyTest()
        {
            var esm = IoCManager.Resolve<IEntitySystemManager>();

            var sysA = esm.GetEntitySystem<ESystemDepA>();
            var sysB = esm.GetEntitySystem<ESystemDepB>();

            Assert.That(sysA.ESystemDepB, Is.EqualTo(sysB));
            Assert.That(sysB.ESystemDepA, Is.EqualTo(sysA));
        }

    }
}
