using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using Robust.Shared.Exceptions;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.ComponentDependencies;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.IoC;

namespace Robust.UnitTesting.Shared.GameObjects
{
    [TestFixture, Parallelizable ,TestOf(typeof(ComponentManager))]
    class ComponentManager_Tests
    {
        private const uint CompNetId = 3;

#region Component Management

        [Test]
        public void AddComponentTest()
        {
            // Arrange
            var manager = ManagerFactory(out var entityManager);
            var entity = EntityFactory(entityManager);
            var component = new DummyComponent();
            component.Owner = entity;

            // Act
            manager.AddComponent(entity, component);

            // Assert
            var result = manager.GetComponent<DummyComponent>(entity.Uid);
            Assert.That(result, Is.EqualTo(component));
        }

        [Test]
        public void AddComponentOverwriteTest()
        {
            // Arrange
            var manager = ManagerFactory(out var entityManager);
            var entity = EntityFactory(entityManager);
            var component = new DummyComponent();
            component.Owner = entity;
            manager.AddComponent(entity, new DummyComponent
            {
                Owner = entity
            });

            // Act
            manager.AddComponent(entity, component, true);

            // Assert
            var result = manager.GetComponent<DummyComponent>(entity.Uid);
            Assert.That(result, Is.EqualTo(component));
        }

        [Test]
        public void HasComponentTest()
        {
            // Arrange
            var manager = ManagerFactory(out var entityManager);
            var entity = EntityFactory(entityManager);
            var component = new DummyComponent();
            component.Owner = entity;
            manager.AddComponent(entity, component);

            // Act
            var result = manager.HasComponent<DummyComponent>(entity.Uid);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void HasNetComponentTest()
        {
            // Arrange
            var manager = ManagerFactory(out var entityManager);
            var entity = EntityFactory(entityManager);
            var component = new DummyComponent();
            component.Owner = entity;
            manager.AddComponent(entity, component);

            // Act
            var result = manager.HasComponent(entity.Uid, CompNetId);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void GetNetComponentTest()
        {
            // Arrange
            var manager = ManagerFactory(out var entityManager);
            var entity = EntityFactory(entityManager);
            var component = new DummyComponent();
            component.Owner = entity;
            manager.AddComponent(entity, component);

            // Act
            var result = manager.GetComponent(entity.Uid, CompNetId);

            // Assert
            Assert.That(result, Is.EqualTo(component));
        }

        [Test]
        public void TryGetComponentTest()
        {
            // Arrange
            var manager = ManagerFactory(out var entityManager);
            var entity = EntityFactory(entityManager);
            var component = new DummyComponent();
            component.Owner = entity;
            manager.AddComponent(entity, component);

            // Act
            var result = manager.TryGetComponent<DummyComponent>(entity.Uid, out var comp);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(comp, Is.EqualTo(component));
        }

        [Test]
        public void TryGetNetComponentTest()
        {
            // Arrange
            var manager = ManagerFactory(out var entityManager);
            var entity = EntityFactory(entityManager);
            var component = new DummyComponent();
            component.Owner = entity;
            manager.AddComponent(entity, component);

            // Act
            var result = manager.TryGetComponent(entity.Uid, CompNetId, out var comp);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(comp, Is.EqualTo(component));
        }

        [Test]
        public void RemoveComponentTest()
        {
            // Arrange
            var manager = ManagerFactory(out var entityManager);
            var entity = EntityFactory(entityManager);
            var component = new DummyComponent();
            component.Owner = entity;
            manager.AddComponent(entity, component);

            // Act
            manager.RemoveComponent<DummyComponent>(entity.Uid);
            manager.CullRemovedComponents();

            // Assert
            Assert.That(manager.HasComponent(entity.Uid, component.GetType()), Is.False);
        }

        [Test]
        public void RemoveNetComponentTest()
        {
            // Arrange
            var manager = ManagerFactory(out var entityManager);
            var entity = EntityFactory(entityManager);
            var component = new DummyComponent();
            component.Owner = entity;
            manager.AddComponent(entity, component);

            // Act
            manager.RemoveComponent(entity.Uid, CompNetId);
            manager.CullRemovedComponents();

            // Assert
            Assert.That(manager.HasComponent(entity.Uid, component.GetType()), Is.False);
        }

        [Test]
        public void GetComponentsTest()
        {
            // Arrange
            var manager = ManagerFactory(out var entityManager);
            var entity = EntityFactory(entityManager);
            var component = new DummyComponent();
            component.Owner = entity;
            manager.AddComponent(entity, component);

            // Act
            var result = manager.GetComponents<DummyComponent>(entity.Uid);

            // Assert
            var list = result.ToList();
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0], Is.EqualTo(component));
        }

        [Test]
        public void GetAllComponentsTest()
        {
            // Arrange
            var manager = ManagerFactory(out var entityManager);
            var entity = EntityFactory(entityManager);
            var component = new DummyComponent();
            component.Owner = entity;
            manager.AddComponent(entity, component);

            // Act
            var result = manager.EntityQuery<DummyComponent>(true);

            // Assert
            var list = result.ToList();
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0], Is.EqualTo(component));
        }

        [Test]
        public void GetAllComponentInstances()
        {
            // Arrange
            var manager = ManagerFactory(out var entityManager);
            var entity = EntityFactory(entityManager);
            var component = new DummyComponent();
            component.Owner = entity;
            manager.AddComponent(entity, component);

            // Act
            var result = manager.GetComponents(entity.Uid);

            // Assert
            var list = result.ToList();
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0], Is.EqualTo(component));
        }

        #endregion

        private static IComponentManager ManagerFactory(out IEntityManager entityManager)
        {
            var dependencies = new DependencyCollection();

            var runtimeLog = new Mock<IRuntimeLog>();
            dependencies.RegisterInstance<IRuntimeLog>(runtimeLog.Object);

            // set up the registration
            var mockRegistration = new Mock<IComponentRegistration>();
            mockRegistration.SetupGet(x => x.References).Returns(new List<Type> { typeof(DummyComponent) });

            // setup the comp factory
            var mockFactory = new Mock<IComponentFactory>();
            mockFactory.Setup(x => x.GetRegistration(It.IsAny<IComponent>())).Returns(mockRegistration.Object);
            mockFactory.Setup(x => x.GetRegistration(It.IsAny<Type>())).Returns(mockRegistration.Object);
            mockFactory.Setup(x => x.GetComponent<DummyComponent>()).Returns(new DummyComponent());
            mockFactory.Setup(x => x.GetAllRefTypes()).Returns(new[] { typeof(DummyComponent) });
            mockFactory.Setup(x => x.GetAllNetIds()).Returns(new[] { CompNetId });
            dependencies.RegisterInstance<IComponentFactory>(mockFactory.Object);
            var mockCompDependencyManager = new Mock<IComponentDependencyManager>();
            dependencies.RegisterInstance<IComponentDependencyManager>(mockCompDependencyManager.Object); //todo probably not correct

            // set up the entity manager
            var mockEntMan = new Mock<IEntityManager>();
            dependencies.RegisterInstance<IEntityManager>(mockEntMan.Object);

            var manager = new ComponentManager();
            dependencies.InjectDependencies(manager);
            manager.Initialize();

            entityManager = mockEntMan.Object;
            return manager;
        }

        private class DummyComponent : Component, ICompType1, ICompType2
        {
            public override string Name => "Dummy";
            public override uint? NetID => CompNetId;
        }

        private static IEntity EntityFactory(IEntityManager entityManager)
        {
            var mockEnt = new Mock<IEntity>();
            mockEnt.SetupGet(x => x.EntityManager).Returns(entityManager);
            mockEnt.SetupGet(x => x.Uid).Returns(new EntityUid(7));
            mockEnt.Setup(x => x.IsValid()).Returns(true);
            return mockEnt.Object;
        }

        private interface ICompType1 { }

        private interface ICompType2 { }
    }
}
