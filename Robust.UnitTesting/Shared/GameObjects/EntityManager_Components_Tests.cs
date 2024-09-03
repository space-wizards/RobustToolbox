using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.GameObjects
{
    [TestFixture, Parallelizable ,TestOf(typeof(EntityManager))]
    public sealed partial class EntityManager_Components_Tests
    {
        private const string DummyLoad = @"
        - type: entity
          id: DummyLoad
          name: weh
          components:
          - type: Joint
          - type: Physics
";

        [Test]
        public void AddRegistryComponentTest()
        {
            var sim = RobustServerSimulation
                .NewSimulation()
                .RegisterPrototypes(fac => fac.LoadString(DummyLoad))
                .InitializeInstance();

            var entMan = sim.Resolve<IEntityManager>();
            var protoManager = sim.Resolve<IPrototypeManager>();

            var map = sim.CreateMap().Uid;
            var coords = new EntityCoordinates(map, default);
            var entity = entMan.SpawnEntity(null, coords);
            Assert.That(!entMan.HasComponent<PhysicsComponent>(entity));
            var proto = protoManager.Index<EntityPrototype>("DummyLoad");

            entMan.AddComponents(entity, proto);
            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<JointComponent>(entity));
                Assert.That(entMan.HasComponent<PhysicsComponent>(entity));
            });
        }

        [Test]
        public void RemoveRegistryComponentTest()
        {
            var sim = RobustServerSimulation
                .NewSimulation()
                .RegisterPrototypes(fac => fac.LoadString(DummyLoad))
                .InitializeInstance();

            var entMan = sim.Resolve<IEntityManager>();
            var protoManager = sim.Resolve<IPrototypeManager>();

            var map = sim.CreateMap().Uid;
            var coords = new EntityCoordinates(map, default);
            var entity = entMan.SpawnEntity("DummyLoad", coords);
            var proto = protoManager.Index<EntityPrototype>("DummyLoad");

            entMan.RemoveComponents(entity, proto);
            Assert.Multiple(() =>
            {
                Assert.That(!entMan.HasComponent<JointComponent>(entity));
                Assert.That(!entMan.HasComponent<PhysicsComponent>(entity));
            });
        }

        [Test]
        public void AddComponentTest()
        {
            // Arrange
            var (sim, coords) = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var entity = entMan.SpawnEntity(null, coords);
            var component = new DummyComponent();

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
            var (sim, coords) = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var entity = entMan.SpawnEntity(null, coords);
            var component = new DummyComponent();

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
            var (sim, coords) = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var entity = entMan.SpawnEntity(null, coords);
            var firstComp = new DummyComponent();
            entMan.AddComponent(entity, firstComp);
            entMan.RemoveComponent<DummyComponent>(entity);
            var secondComp = new DummyComponent();

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
            var (sim, coords) = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var entity = entMan.SpawnEntity(null, coords);
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
            var (sim, coords) = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var entity = entMan.SpawnEntity(null, coords);
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
            var (sim, coords) = SimulationFactory();

            var factory = sim.Resolve<IComponentFactory>();
            var netId = factory.GetRegistration<DummyComponent>().NetID!;

            var entMan = sim.Resolve<IEntityManager>();
            var entity = entMan.SpawnEntity(null, coords);
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
            var (sim, coords) = SimulationFactory();

            var factory = sim.Resolve<IComponentFactory>();
            var netId = factory.GetRegistration<DummyComponent>().NetID!;

            var entMan = sim.Resolve<IEntityManager>();
            var entity = entMan.SpawnEntity(null, coords);
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
            var (sim, coords) = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var entity = entMan.SpawnEntity(null, coords);
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
            var (sim, coords) = SimulationFactory();

            var factory = sim.Resolve<IComponentFactory>();
            var netId = factory.GetRegistration<DummyComponent>().NetID!;

            var entMan = sim.Resolve<IEntityManager>();
            var entity = entMan.SpawnEntity(null, coords);
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
            var (sim, coords) = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var entity = entMan.SpawnEntity(null, coords);
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
            var (sim, coords) = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var entity = entMan.SpawnEntity(null, coords);
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
            var (sim, coords) = SimulationFactory();

            var factory = sim.Resolve<IComponentFactory>();
            var netId = factory.GetRegistration<DummyComponent>().NetID!;

            var entMan = sim.Resolve<IEntityManager>();
            var entity = entMan.SpawnEntity(null, coords);
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
            var (sim, coords) = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var entity = entMan.SpawnEntity(null, coords);
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
            var (sim, coords) = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var entity = entMan.SpawnEntity(null, coords);
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
            var (sim, coords) = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var fac = sim.Resolve<IComponentFactory>();
            var entity = entMan.SpawnEntity(null, coords);
            var component = entMan.AddComponent<DummyComponent>(entity);

            // Act
            var result = entMan.GetComponents(entity);

            // Assert
            var list = result.Where(c=>fac.GetComponentName(c.GetType()) == "Dummy").ToList();
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0], Is.EqualTo(component));
        }

        private static (ISimulation, EntityCoordinates) SimulationFactory()
        {
            var sim = RobustServerSimulation
                .NewSimulation()
                .RegisterComponents(factory => factory.RegisterClass<DummyComponent>())
                .InitializeInstance();

            var map = sim.CreateMap().Uid;
            var coords = new EntityCoordinates(map, default);
            return (sim, coords);
        }

        [NetworkedComponent()]
        private sealed partial class DummyComponent : Component, ICompType1, ICompType2
        {
        }

        private interface ICompType1 { }

        private interface ICompType2 { }
    }
}
