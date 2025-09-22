using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Server.GameObjects.Components
{
    [TestFixture, Parallelizable]
    public sealed partial class ContainerTest
    {
        private static EntityCoordinates _coords;

        private static ISimulation SimulationFactory()
        {
            var sim = RobustServerSimulation
                .NewSimulation()
                .InitializeInstance();
            var map = sim.CreateMap();
            _coords = new EntityCoordinates(map.Item1, default);

            return sim;
        }

        [Test]
        public void TestCreation()
        {
            var sim = SimulationFactory();
            var entManager = sim.Resolve<IEntityManager>();
            var containerSys = sim.Resolve<IEntitySystemManager>().GetEntitySystem<ContainerSystem>();
            var entity = sim.SpawnEntity(null,_coords);

            var container = containerSys.MakeContainer<Container>(entity, "dummy");

            Assert.That(container.ID, Is.EqualTo("dummy"));
            Assert.That(container.Owner, Is.EqualTo(entity));

            var manager = entManager.GetComponent<ContainerManagerComponent>(entity);

            Assert.That(container.Manager, Is.EqualTo(manager));
            Assert.That(() => containerSys.MakeContainer<Container>(entity, "dummy"), Throws.ArgumentException);

            Assert.That(containerSys.HasContainer(entity, "dummy2", manager), Is.False);
            var container2 = containerSys.MakeContainer<Container>(entity, "dummy2");

            Assert.That(container2.Manager, Is.EqualTo(manager));
            Assert.That(container2.Owner, Is.EqualTo(entity));
            Assert.That(container2.ID, Is.EqualTo("dummy2"));

            Assert.That(containerSys.HasContainer(entity, "dummy", manager), Is.True);
            Assert.That(containerSys.HasContainer(entity, "dummy2",manager), Is.True);
            Assert.That(containerSys.HasContainer(entity, "dummy3", manager), Is.False);

            Assert.That(containerSys.GetContainer(entity, "dummy", manager), Is.EqualTo(container));
            Assert.That(containerSys.GetContainer(entity, "dummy2", manager), Is.EqualTo(container2));
            Assert.That(() => containerSys.GetContainer(entity, "dummy3", manager), Throws.TypeOf<KeyNotFoundException>());

            entManager.DeleteEntity(entity);
        }

        [Test]
        public void TestInsertion()
        {
            var sim = SimulationFactory();
            var entManager = sim.Resolve<IEntityManager>();
            var containerSys = sim.Resolve<IEntitySystemManager>().GetEntitySystem<ContainerSystem>();
            var owner = sim.SpawnEntity(null,_coords);
            var inserted = sim.SpawnEntity(null,_coords);
            var transform = entManager.GetComponent<TransformComponent>(inserted);

            var container = containerSys.MakeContainer<Container>(owner, "dummy");
            Assert.That(containerSys.Insert(inserted, container), Is.True);
            Assert.That(transform.ParentUid, Is.EqualTo(owner));

            var container2 = containerSys.MakeContainer<Container>(inserted, "dummy");
            Assert.That(containerSys.Insert(owner, container2), Is.False);

            var success = containerSys.Remove(inserted, container);
            Assert.That(success, Is.True);

            success = containerSys.Remove(inserted, container);
            Assert.That(success, Is.False);

            containerSys.Insert(inserted, container);
            entManager.DeleteEntity(owner);
            // Make sure inserted was detached.
            Assert.That(transform.Deleted, Is.True);
        }

        [Test]
        public void TestNestedRemoval()
        {
            var sim = SimulationFactory();
            var entManager = sim.Resolve<IEntityManager>();
            var containerSys = sim.Resolve<IEntitySystemManager>().GetEntitySystem<ContainerSystem>();
            var owner = sim.SpawnEntity(null,_coords);
            var inserted = sim.SpawnEntity(null,_coords);
            var transform = entManager.GetComponent<TransformComponent>(inserted);
            var entity = sim.SpawnEntity(null,_coords);

            var container = containerSys.MakeContainer<Container>(owner, "dummy");
            Assert.That(containerSys.Insert(inserted, container), Is.True);
            Assert.That(transform.ParentUid, Is.EqualTo(owner));

            var container2 = containerSys.MakeContainer<Container>(inserted, "dummy");
            Assert.That(containerSys.Insert(entity, container2), Is.True);
            Assert.That(entManager.GetComponent<TransformComponent>(entity).ParentUid, Is.EqualTo(inserted));

            Assert.That(containerSys.Remove(entity, container2), Is.True);
            Assert.That(container.Contains(entity), Is.True);
            Assert.That(entManager.GetComponent<TransformComponent>(entity).ParentUid, Is.EqualTo(owner));

            entManager.DeleteEntity(owner);
            Assert.That(transform.Deleted, Is.True);
        }

        [Test]
        public void TestNestedRemovalWithDenial()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var containerSys = sim.Resolve<IEntitySystemManager>().GetEntitySystem<ContainerSystem>();
            var coordinates =_coords;
            var entityOne = sim.SpawnEntity(null, coordinates);
            var entityTwo = sim.SpawnEntity(null, coordinates);
            var entityThree = sim.SpawnEntity(null, coordinates);
            var entityItem = sim.SpawnEntity(null, coordinates);

            var container = containerSys.MakeContainer<Container>(entityOne, "dummy");
            var container2 = containerSys.MakeContainer<ContainerOnlyContainer>(entityTwo, "dummy");
            var container3 = containerSys.MakeContainer<Container>(entityThree, "dummy");

            Assert.That(containerSys.Insert(entityTwo, container), Is.True);
            Assert.That(entMan.GetComponent<TransformComponent>(entityTwo).ParentUid, Is.EqualTo(entityOne));

            Assert.That(containerSys.Insert(entityThree, container2), Is.True);
            Assert.That(entMan.GetComponent<TransformComponent>(entityThree).ParentUid, Is.EqualTo(entityTwo));

            Assert.That(containerSys.Insert(entityItem, container3), Is.True);
            Assert.That(entMan.GetComponent<TransformComponent>(entityItem).ParentUid, Is.EqualTo(entityThree));

            Assert.That(containerSys.Remove(entityItem, container3), Is.True);
            Assert.That(container.Contains(entityItem), Is.True);
            Assert.That(entMan.GetComponent<TransformComponent>(entityItem).ParentUid, Is.EqualTo(entityOne));

            entMan.DeleteEntity(entityOne);
            Assert.That(entMan.Deleted(entityOne), Is.True);
        }

        [Test]
        public void BaseContainer_SelfInsert_False()
        {
            var sim = SimulationFactory();
            var containerSys = sim.Resolve<IEntitySystemManager>().GetEntitySystem<ContainerSystem>();
            var entity = sim.SpawnEntity(null,_coords);
            var container = containerSys.MakeContainer<Container>(entity, "dummy");

            Assert.That(containerSys.Insert(entity, container), Is.False);
            Assert.That(containerSys.CanInsert(entity, container), Is.False);
        }

        [Test]
        public void BaseContainer_InsertMap_False()
        {
            var sim = SimulationFactory();
            var containerSys = sim.Resolve<IEntitySystemManager>().GetEntitySystem<ContainerSystem>();
            var mapEnt = new EntityUid(1);
            var entity = sim.SpawnEntity(null,_coords);
            var container = containerSys.MakeContainer<Container>(entity, "dummy");

            Assert.That(containerSys.Insert(mapEnt, container), Is.False);
            Assert.That(containerSys.CanInsert(mapEnt, container), Is.False);
        }

        [Test]
        public void BaseContainer_InsertGrid_False()
        {
            var sim = SimulationFactory();
            var containerSys = sim.Resolve<IEntitySystemManager>().GetEntitySystem<ContainerSystem>();

            var grid = sim.Resolve<IMapManager>().CreateGridEntity(new MapId(1)).Owner;
            var entity = sim.SpawnEntity(null,_coords);
            var container = containerSys.MakeContainer<Container>(entity, "dummy");

            Assert.That(containerSys.Insert(grid, container), Is.False);
            Assert.That(containerSys.CanInsert(grid, container), Is.False);
        }

        [Test]
        public void BaseContainer_Insert_True()
        {
            var sim = SimulationFactory();
            var entManager = sim.Resolve<IEntityManager>();
            var containerSys = sim.Resolve<IEntitySystemManager>().GetEntitySystem<ContainerSystem>();
            var containerEntity = sim.SpawnEntity(null,_coords);
            var container = containerSys.MakeContainer<Container>(containerEntity, "dummy");
            var insertEntity = sim.SpawnEntity(null,_coords);

            var result = containerSys.Insert(insertEntity, container);

            Assert.That(result, Is.True);
            Assert.That(container.ContainedEntities.Count, Is.EqualTo(1));

            Assert.That(entManager.GetComponent<TransformComponent>(containerEntity).ChildCount, Is.EqualTo(1));
            Assert.That(entManager.GetComponent<TransformComponent>(containerEntity)._children.First(), Is.EqualTo(insertEntity));

            result = containerSys.TryGetContainingContainer(insertEntity, out var resultContainerMan);
            Assert.That(result, Is.True);
            Assert.That(resultContainerMan?.Manager, Is.EqualTo(container.Manager));
        }

        [Test]
        public void BaseContainer_RemoveNotAdded_False()
        {
            var sim = SimulationFactory();
            var containerSys = sim.Resolve<IEntitySystemManager>().GetEntitySystem<ContainerSystem>();
            var containerEntity = sim.SpawnEntity(null,_coords);
            var container = containerSys.MakeContainer<Container>(containerEntity, "dummy");
            var insertEntity = sim.SpawnEntity(null,_coords);

            var result = containerSys.Remove(insertEntity, container);

            Assert.That(result, Is.False);
        }

        [Test]
        public void BaseContainer_Transfer_True()
        {
            var sim = SimulationFactory();
            var containerSys = sim.Resolve<IEntitySystemManager>().GetEntitySystem<ContainerSystem>();
            var entity1 = sim.SpawnEntity(null,_coords);
            var container1 = containerSys.MakeContainer<Container>(entity1, "dummy");
            var entity2 = sim.SpawnEntity(null,_coords);
            var container2 = containerSys.MakeContainer<Container>(entity2, "dummy");
            var transferEntity = sim.SpawnEntity(null,_coords);
            containerSys.Insert(transferEntity, container1);

            var result = containerSys.Insert(transferEntity, container2);

            Assert.That(result, Is.True);
            Assert.That(container1.ContainedEntities.Count, Is.EqualTo(0));
            Assert.That(container2.ContainedEntities.Count, Is.EqualTo(1));
        }

        [Test]
        public void Container_Serialize()
        {
            var sim = SimulationFactory();
            var entManager = sim.Resolve<IEntityManager>();
            var containerSys = entManager.System<ContainerSystem>();
            var entity = sim.SpawnEntity(null,_coords);
            var container = containerSys.MakeContainer<Container>(entity, "dummy");
            var childEnt = sim.SpawnEntity(null,_coords);

            container.OccludesLight = true;
            container.ShowContents = true;
            containerSys.Insert(childEnt, container);

            var containerMan = entManager.GetComponent<ContainerManagerComponent>(entity);
            var getState = new ComponentGetState();
            entManager.EventBus.RaiseComponentEvent(entity, containerMan, ref getState);
            var state = (ContainerManagerComponent.ContainerManagerComponentState)getState.State!;

            Assert.That(state.Containers, Has.Count.EqualTo(1));
            var cont = state.Containers.Values.First();
            Assert.That(state.Containers.Keys.First(), Is.EqualTo("dummy"));
            Assert.That(cont.OccludesLight, Is.True);
            Assert.That(cont.ShowContents, Is.True);
            Assert.That(cont.ContainedEntities.Count, Is.EqualTo(1));
            Assert.That(cont.ContainedEntities[0], Is.EqualTo(entManager.GetNetEntity(childEnt)));
        }

        [SerializedType(nameof(ContainerOnlyContainer))]
        private sealed partial class ContainerOnlyContainer : BaseContainer
        {
            /// <summary>
            /// The generic container class uses a list of entities
            /// </summary>
            private readonly List<EntityUid> _containerList = new();

            public override int Count => _containerList.Count;

            /// <inheritdoc />
            public override IReadOnlyList<EntityUid> ContainedEntities => _containerList;

            /// <inheritdoc />
            protected internal override void InternalInsert(EntityUid toInsert, IEntityManager entMan)
            {
                _containerList.Add(toInsert);
            }

            /// <inheritdoc />
            protected internal override void InternalRemove(EntityUid toRemove, IEntityManager entMan)
            {
                _containerList.Remove(toRemove);
            }

            /// <inheritdoc />
            public override bool Contains(EntityUid contained)
            {
                if (!_containerList.Contains(contained))
                    return false;

                if (IoCManager.Resolve<IGameTiming>().ApplyingState)
                    return true;

                var flags = IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(contained).Flags;
                DebugTools.Assert((flags & MetaDataFlags.InContainer) != 0);
                return true;
            }

            /// <inheritdoc />
            protected internal override void InternalShutdown(IEntityManager entMan, SharedContainerSystem system, bool isClient)
            {
                foreach (var entity in _containerList.ToArray())
                {
                    if (!isClient)
                        entMan.DeleteEntity(entity);
                    else if (entMan.EntityExists(entity))
                        system.Remove(entity, this, reparent: false, force: true);
                }
            }

            protected internal override bool CanInsert(EntityUid toinsert, bool assumeEmpty, IEntityManager entMan)
            {
                return entMan.HasComponent<ContainerManagerComponent>(toinsert);
            }
        }
    }
}
