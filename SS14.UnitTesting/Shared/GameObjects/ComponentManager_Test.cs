using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Moq;
using NUnit.Framework;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;

namespace SS14.UnitTesting.Shared.GameObjects
{
    [TestFixture]
    [TestOf(typeof(ComponentManager))]
    class ComponentManager_Test
    {
        [Test]
        public void AddComponentTest()
        {
            // Arrange
            var manager = ManagerFactory();

            var mockComponent = new Mock<IComponent>();
            var component = mockComponent.Object;

            // Act
            manager.AddComponent(component);

            // Assert
            var result = manager.GetComponents<IComponent>().ToList();
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0], Is.EqualTo(component));
        }

        [Test]
        public void DeleteComponentTest()
        {
            // Arrange
            var manager = ManagerFactory();

            var mockComponent = new Mock<IComponent>();
            mockComponent.SetupGet(c => c.Deleted).Returns(true); // mark comp for deletion
            var component = mockComponent.Object;
            manager.AddComponent(component);

            // Act
            manager.Update(0); // update removes components marked for deletion

            // Assert
            var result = manager.GetComponents<IComponent>().ToList();
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void CullTest()
        {
            // Arrange
            var manager = ManagerFactory();

            var mockComponent = new Mock<IComponent>();
            var component = mockComponent.Object;
            manager.AddComponent(component);

            // Act
            manager.Cull(); // deletes all components, regardless of Delete flag.

            // Assert
            var result = manager.GetComponents<IComponent>().ToList();
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void UpdateTest()
        {
            // Arrange
            var manager = ManagerFactory();

            var mockComponent = new Mock<IComponent>();
            var component = mockComponent.Object;
            manager.AddComponent(component);

            // Act
            manager.Update(0.1f);

            // Assert
            mockComponent.Verify(x => x.Update(It.IsIn(0.1f)), Times.Once);
        }

        // mimics the IoC system.
        private static IComponentManager ManagerFactory()
        {
            var manager = new ComponentManager();

            // set up the registration
            var mockRegistration = new Mock<IComponentRegistration>();
            mockRegistration.SetupGet(x => x.References).Returns(new List<Type> { typeof(IComponent) });

            // setup the comp factory
            var mockFactory = new Mock<IComponentFactory>();
            mockFactory.Setup(x => x.GetRegistration(It.IsAny<IComponent>())).Returns(mockRegistration.Object);

            // Inject the dependency into manager
            foreach (var field in GetDepFields(typeof(ComponentManager)))
            {
                if (!field.FieldType.IsAssignableFrom(typeof(IComponentFactory)))
                    continue;

                field.SetValue(manager, mockFactory.Object);
                break;
            }

            // call PostInject on manager
            ((IPostInjectInit)manager).PostInject();

            return manager;
        }

        private static IEnumerable<FieldInfo> GetDepFields(Type targetType)
        {
            return targetType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(p => Attribute.GetCustomAttribute(p, typeof(DependencyAttribute)) != null);
        }
    }
}
