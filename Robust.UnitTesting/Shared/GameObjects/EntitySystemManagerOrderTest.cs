using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Systems;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Robust.UnitTesting.Shared.GameObjects
{
    [TestFixture]
    [TestOf(typeof(EntitySystemManager))]
    public class EntitySystemManagerOrderTest
    {
        private class Counter
        {
            public int X;
        }

        private abstract class TestSystemBase : IEntitySystem
        {
            public Counter? Counter;
            public int LastUpdate;

            public virtual IEnumerable<Type> UpdatesAfter => Enumerable.Empty<Type>();
            public virtual IEnumerable<Type> UpdatesBefore => Enumerable.Empty<Type>();
            public void Initialize() { }
            public void Shutdown() { }

            public void Update(float frameTime)
            {
                LastUpdate = Counter!.X++;
            }
            public void FrameUpdate(float frameTime) { }
        }

        // Expected update order is is A -> D -> C -> B

        private class TestSystemA : TestSystemBase
        {

        }

        private class TestSystemB : TestSystemBase
        {
            public override IEnumerable<Type> UpdatesAfter => new[] {typeof(TestSystemA)};
        }

        private class TestSystemC : TestSystemBase
        {
            public override IEnumerable<Type> UpdatesBefore => new[] {typeof(TestSystemB)};
        }

        private class TestSystemD : TestSystemBase
        {
            public override IEnumerable<Type> UpdatesAfter => new[] {typeof(TestSystemA)};
            public override IEnumerable<Type> UpdatesBefore => new[] {typeof(TestSystemC)};
        }

        [Test]
        public void Test()
        {
            var deps = new DependencyCollection();
            deps.Register<ILogManager, LogManager>();
            deps.Register<IDynamicTypeFactory, DynamicTypeFactory>();
            deps.Register<IEntitySystemManager, EntitySystemManager>();
            deps.RegisterInstance<IEntityManager>(new Mock<IEntityManager>().Object);

            var reflectionMock = new Mock<IReflectionManager>();
            reflectionMock.Setup(p => p.GetAllChildren<IEntitySystem>(false))
                .Returns(new[]
                {
                    typeof(TestSystemA),
                    typeof(TestSystemB),
                    typeof(TestSystemC),
                    typeof(TestSystemD),
                });

            deps.RegisterInstance<IReflectionManager>(reflectionMock.Object);

            deps.BuildGraph();

            IoCManager.InitThread(deps, true);

            var systems = deps.Resolve<IEntitySystemManager>();
            systems.Initialize();

            var counter = new Counter();

            systems.GetEntitySystem<TestSystemA>().Counter = counter;
            systems.GetEntitySystem<TestSystemB>().Counter = counter;
            systems.GetEntitySystem<TestSystemC>().Counter = counter;
            systems.GetEntitySystem<TestSystemD>().Counter = counter;

            systems.Update(1);

            Assert.AreEqual(counter.X, 4);

            Assert.AreEqual(systems.GetEntitySystem<TestSystemA>().LastUpdate, 0);
            Assert.AreEqual(systems.GetEntitySystem<TestSystemB>().LastUpdate, 3);
            Assert.AreEqual(systems.GetEntitySystem<TestSystemC>().LastUpdate, 2);
            Assert.AreEqual(systems.GetEntitySystem<TestSystemD>().LastUpdate, 1);
        }

        [TearDown]
        public void TearDown()
        {
            IoCManager.Clear();
        }
    }
}
