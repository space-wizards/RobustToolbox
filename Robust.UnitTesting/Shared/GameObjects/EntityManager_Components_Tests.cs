using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.GameObjects
{
    [TestFixture, Parallelizable ,TestOf(typeof(EntityManager))]
    public sealed partial class EntityManager_Components_Tests
    {
        private static readonly EntityCoordinates DefaultCoords = new(EntityUid.FirstUid, Vector2.Zero);

        [Test]
        public void AddComponentTest()
        {
            // Arrange
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var entity = entMan.SpawnEntity(null, DefaultCoords);
            var component = new DummyComponent()
            {
                Owner = entity
            };

            // Act
            entMan.AddComponent(entity, component);

            // Assert
            var result = entMan.GetComponent<DummyComponent>(entity);
            Assert.That(result, Is.EqualTo(component));
        }

        [Test]
        public void AddComponentOverwriteTest()
        {
            // Arrange
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var entity = entMan.SpawnEntity(null, DefaultCoords);
            var component = new DummyComponent()
            {
                Owner = entity
            };

            // Act
            entMan.AddComponent(entity, component, true);

            // Assert
            var result = entMan.GetComponent<DummyComponent>(entity);
            Assert.That(result, Is.EqualTo(component));
        }

        [Test]
        public void AddComponent_ExistingDeleted()
        {
            // Arrange
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var entity = entMan.SpawnEntity(null, DefaultCoords);
            var firstComp = new DummyComponent {Owner = entity};
            entMan.AddComponent(entity, firstComp);
            entMan.RemoveComponent<DummyComponent>(entity);
            var secondComp = new DummyComponent { Owner = entity };

            // Act
            entMan.AddComponent(entity, secondComp);

            // Assert
            var result = entMan.GetComponent<DummyComponent>(entity);
            Assert.That(result, Is.EqualTo(secondComp));
        }

        [Test]
        public void HasComponentTest()
        {
            // Arrange
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var entity = entMan.SpawnEntity(null, DefaultCoords);
            entMan.AddComponent<DummyComponent>(entity);

            // Act
            var result = entMan.HasComponent<DummyComponent>(entity);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void HasComponentNoGenericTest()
        {
            // Arrange
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var entity = entMan.SpawnEntity(null, DefaultCoords);
            entMan.AddComponent<DummyComponent>(entity);

            // Act
            var result = entMan.HasComponent(entity, typeof(DummyComponent));

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void HasNetComponentTest()
        {
            // Arrange
            var sim = SimulationFactory();

            var factory = sim.Resolve<IComponentFactory>();
            var netId = factory.GetRegistration<DummyComponent>().NetID!;

            var entMan = sim.Resolve<IEntityManager>();
            var entity = entMan.SpawnEntity(null, DefaultCoords);
            entMan.AddComponent<DummyComponent>(entity);

            // Act
            var result = entMan.HasComponent(entity, netId.Value);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void GetNetComponentTest()
        {
            // Arrange
            var sim = SimulationFactory();

            var factory = sim.Resolve<IComponentFactory>();
            var netId = factory.GetRegistration<DummyComponent>().NetID!;

            var entMan = sim.Resolve<IEntityManager>();
            var entity = entMan.SpawnEntity(null, DefaultCoords);
            var component = entMan.AddComponent<DummyComponent>(entity);

            // Act
            var result = entMan.GetComponent(entity, netId.Value);

            // Assert
            Assert.That(result, Is.EqualTo(component));
        }

        [Test]
        public void TryGetComponentTest()
        {
            // Arrange
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var entity = entMan.SpawnEntity(null, DefaultCoords);
            var component = entMan.AddComponent<DummyComponent>(entity);

            // Act
            var result = entMan.TryGetComponent<DummyComponent>(entity, out var comp);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(comp, Is.EqualTo(component));
        }

        [Test]
        public void TryGetNetComponentTest()
        {
            // Arrange
            var sim = SimulationFactory();

            var factory = sim.Resolve<IComponentFactory>();
            var netId = factory.GetRegistration<DummyComponent>().NetID!;

            var entMan = sim.Resolve<IEntityManager>();
            var entity = entMan.SpawnEntity(null, DefaultCoords);
            var component = entMan.AddComponent<DummyComponent>(entity);

            // Act
            var result = entMan.TryGetComponent(entity, netId.Value, out var comp);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(comp, Is.EqualTo(component));
        }

        [Test]
        public void RemoveComponentTest()
        {
            // Arrange
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var entity = entMan.SpawnEntity(null, DefaultCoords);
            var component = entMan.AddComponent<DummyComponent>(entity);

            // Act
            entMan.RemoveComponent<DummyComponent>(entity);
            entMan.CullRemovedComponents();

            // Assert
            Assert.That(entMan.HasComponent(entity, component.GetType()), Is.False);
        }

        [Test]
        public void EnsureQueuedComponentDeletion()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var entity = entMan.SpawnEntity(null, DefaultCoords);
            var component = entMan.AddComponent<DummyComponent>(entity);

            Assert.That(component.LifeStage, Is.LessThanOrEqualTo(ComponentLifeStage.Running));
            entMan.RemoveComponentDeferred(entity, component);
            Assert.That(component.LifeStage, Is.EqualTo(ComponentLifeStage.Stopped));

            Assert.That(entMan.EnsureComponent<DummyComponent>(entity, out var comp2), Is.False);
            Assert.That(comp2.LifeStage, Is.LessThanOrEqualTo(ComponentLifeStage.Running));
            Assert.That(component.LifeStage, Is.EqualTo(ComponentLifeStage.Deleted));
        }

        [Test]
        public void RemoveNetComponentTest()
        {
            // Arrange
            var sim = SimulationFactory();

            var factory = sim.Resolve<IComponentFactory>();
            var netId = factory.GetRegistration<DummyComponent>().NetID!;

            var entMan = sim.Resolve<IEntityManager>();
            var entity = entMan.SpawnEntity(null, DefaultCoords);
            var component = entMan.AddComponent<DummyComponent>(entity);

            // Act
            entMan.RemoveComponent(entity, netId.Value);
            entMan.CullRemovedComponents();

            // Assert
            Assert.That(entMan.HasComponent(entity, component.GetType()), Is.False);
        }

        [Test]
        public void GetComponentsTest()
        {
            // Arrange
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var entity = entMan.SpawnEntity(null, DefaultCoords);
            var component = entMan.AddComponent<DummyComponent>(entity);

            // Act
            var result = entMan.GetComponents<DummyComponent>(entity);

            // Assert
            var list = result.ToList();
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0], Is.EqualTo(component));
        }

        [Test]
        public void GetAllComponentsTest()
        {
            // Arrange
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var entity = entMan.SpawnEntity(null, DefaultCoords);
            var component = entMan.AddComponent<DummyComponent>(entity);

            // Act
            var result = entMan.EntityQuery<DummyComponent>(true);

            // Assert
            var list = result.ToList();
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0], Is.EqualTo(component));
        }

        [Test]
        public void GetAllComponentInstances()
        {
            // Arrange
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var fac = sim.Resolve<IComponentFactory>();
            var entity = entMan.SpawnEntity(null, DefaultCoords);
            var component = entMan.AddComponent<DummyComponent>(entity);

            // Act
            var result = entMan.GetComponents(entity);

            // Assert
            var list = result.Where(c=>fac.GetComponentName(c.GetType()) == "Dummy").ToList();
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0], Is.EqualTo(component));
        }

        private static ISimulation SimulationFactory()
        {
            var sim = RobustServerSimulation
                .NewSimulation()
                .RegisterComponents(factory => factory.RegisterClass<DummyComponent>())
                .InitializeInstance();

            // Adds the map with id 1, and spawns entity 1 as the map entity.
            sim.AddMap(1);

            return sim;
        }

        [NetworkedComponent()]
        private sealed partial class DummyComponent : Component, ICompType1, ICompType2
        {
        }

        private interface ICompType1 { }

        private interface ICompType2 { }
    }
}
