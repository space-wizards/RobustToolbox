using Moq;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Robust.UnitTesting.Shared.GameObjects
{
    [TestOf(typeof(ComponentFactory))]
    public class ComponentFactoryTest
    {
        private IComponentFactory _factory = default!;

        [SetUp]
        public void Setup()
        {
            var container = new DependencyCollection();
            container.Register<ILogManager, LogManager>();
            container.Register<IDynamicTypeFactory, DynamicTypeFactory>();
            container.Register<IComponentFactory, ComponentFactory>();

            var reflectionMock = new Mock<IReflectionManager>();
            reflectionMock.Setup(p => p.FindTypesWithAttribute<RegisterComponentAttribute>())
                .Returns(new[]
                {
                    typeof(TestA),
                    typeof(TestB),
                    typeof(TestC),
                    typeof(TestE),
                });

            container.RegisterInstance<IReflectionManager>(reflectionMock.Object);

            container.BuildGraph();
            _factory = container.Resolve<IComponentFactory>();
        }

        [Test]
        public void TestAutoReg()
        {
            _factory.DoAutoRegistrations();

            var reg = _factory.GetRegistration("A");
            Assert.That(reg.Type, Is.EqualTo(typeof(TestA)));
            Assert.That(reg.References, Is.EquivalentTo(new[] {typeof(TestA), typeof(IInterfaceA)}));

            reg = _factory.GetRegistration("B");
            Assert.That(reg.Type, Is.EqualTo(typeof(TestB)));
            Assert.That(reg.References, Is.EquivalentTo(new[] {typeof(TestB), typeof(TestA), typeof(IInterfaceA)}));

            // Recursive disabled, don't inherit refs.
            reg = _factory.GetRegistration("C");
            Assert.That(reg.Type, Is.EqualTo(typeof(TestC)));
            Assert.That(reg.References, Is.EquivalentTo(new[] {typeof(TestC)}));

            // TestD doesn't have [RegisterComponent] so recursive doesn't catch it.
            reg = _factory.GetRegistration("E");
            Assert.That(reg.Type, Is.EqualTo(typeof(TestE)));
            Assert.That(reg.References, Is.EquivalentTo(new[] {typeof(TestE)}));
        }


        [RegisterComponent]
        [ComponentReference(typeof(IInterfaceA))]
        private class TestA : Component, IInterfaceA, IInterfaceB
        {
            public override string Name => "A";
        }

        [RegisterComponent]
        private class TestB : TestA
        {
            public override string Name => "B";
        }

        [RegisterComponent(Recursive = false)]
        private class TestC : TestA
        {
            public override string Name => "C";
        }

        private class TestD : Component
        {
            public override string Name => "D";
        }

        [RegisterComponent]
        private class TestE : TestD
        {
            public override string Name => "E";
        }


        private interface IInterfaceA
        {
        }

        private interface IInterfaceB
        {
        }
    }
}
