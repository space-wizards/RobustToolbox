using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Reflection;

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
            deps.Register<IDynamicTypeFactoryInternal, DynamicTypeFactory>();
            deps.RegisterInstance<IModLoader>(new Mock<IModLoader>().Object);
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

            systems.TickUpdate(1);

            Assert.That(counter.X, Is.EqualTo(4));

            Assert.That(systems.GetEntitySystem<TestSystemA>().LastUpdate, Is.EqualTo(0));
            Assert.That(systems.GetEntitySystem<TestSystemB>().LastUpdate, Is.EqualTo(3));
            Assert.That(systems.GetEntitySystem<TestSystemC>().LastUpdate, Is.EqualTo(2));
            Assert.That(systems.GetEntitySystem<TestSystemD>().LastUpdate, Is.EqualTo(1));
        }

        [TearDown]
        public void TearDown()
        {
            IoCManager.Clear();
        }
    }
}
