using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Robust.UnitTesting.Server.GameObjects.Components
{
    [TestFixture]
    public class ContainerTest : RobustUnitTest
    {
        private IServerEntityManager EntityManager = default!;

        const string PROTOTYPES = @"
- type: entity
  name: dummy
  id: dummy
  components:
  - type: Transform
";

        [OneTimeSetUp]
        public void Setup()
        {
            var compMan = IoCManager.Resolve<IComponentManager>();
            compMan.Initialize();
            EntityManager = IoCManager.Resolve<IServerEntityManager>();

            var mapManager = IoCManager.Resolve<IMapManager>();
            mapManager.Initialize();
            mapManager.Startup();

            mapManager.CreateMap();

            var manager = IoCManager.Resolve<IPrototypeManager>();
            manager.LoadFromStream(new StringReader(PROTOTYPES));
            manager.Resync();
        }

        [Test]
        public void TestCreation()
        {
            var entity = EntityManager.SpawnEntity("dummy", new EntityCoordinates(new EntityUid(1), (0, 0)));

            var container = ContainerManagerComponent.Create<Container>("dummy", entity);

            Assert.That(container.ID, Is.EqualTo("dummy"));
            Assert.That(container.Owner, Is.EqualTo(entity));

            var manager = entity.GetComponent<IContainerManager>();

            Assert.That(container.Manager, Is.EqualTo(manager));
            Assert.That(() => ContainerManagerComponent.Create<Container>("dummy", entity), Throws.ArgumentException);

            Assert.That(manager.HasContainer("dummy2"), Is.False);
            var container2 = ContainerManagerComponent.Create<Container>("dummy2", entity);

            Assert.That(container2.Manager, Is.EqualTo(manager));
            Assert.That(container2.Owner, Is.EqualTo(entity));
            Assert.That(container2.ID, Is.EqualTo("dummy2"));

            Assert.That(manager.HasContainer("dummy"), Is.True);
            Assert.That(manager.HasContainer("dummy2"), Is.True);
            Assert.That(manager.HasContainer("dummy3"), Is.False);

            Assert.That(manager.GetContainer("dummy"), Is.EqualTo(container));
            Assert.That(manager.GetContainer("dummy2"), Is.EqualTo(container2));
            Assert.That(() => manager.GetContainer("dummy3"), Throws.TypeOf<KeyNotFoundException>());

            entity.Delete();

            Assert.That(manager.Deleted, Is.True);
            Assert.That(container.Deleted, Is.True);
            Assert.That(container2.Deleted, Is.True);
        }

        [Test]
        public void TestInsertion()
        {
            var owner = EntityManager.SpawnEntity("dummy", new EntityCoordinates(new EntityUid(1), (0, 0)));
            var inserted = EntityManager.SpawnEntity("dummy", new EntityCoordinates(new EntityUid(1), (0, 0)));
            var transform = inserted.Transform;

            var container = ContainerManagerComponent.Create<Container>("dummy", owner);
            Assert.That(container.Insert(inserted), Is.True);
            Assert.That(transform.Parent!.Owner, Is.EqualTo(owner));

            var container2 = ContainerManagerComponent.Create<Container>("dummy", inserted);
            Assert.That(container2.Insert(owner), Is.False);

            var success = container.Remove(inserted);
            Assert.That(success, Is.True);

            success = container.Remove(inserted);
            Assert.That(success, Is.False);

            container.Insert(inserted);
            owner.Delete();
            // Make sure inserted was detached.
            Assert.That(transform.Deleted, Is.True);
        }

        [Test]
        public void TestNestedRemoval()
        {
            var owner = EntityManager.SpawnEntity("dummy", new EntityCoordinates(new EntityUid(1), (0, 0)));
            var inserted = EntityManager.SpawnEntity("dummy", new EntityCoordinates(new EntityUid(1), (0, 0)));
            var transform = inserted.Transform;
            var entity = EntityManager.SpawnEntity("dummy", new EntityCoordinates(new EntityUid(1), (0, 0)));

            var container = ContainerManagerComponent.Create<Container>("dummy", owner);
            Assert.That(container.Insert(inserted), Is.True);
            Assert.That(transform.Parent!.Owner, Is.EqualTo(owner));

            var container2 = ContainerManagerComponent.Create<Container>("dummy", inserted);
            Assert.That(container2.Insert(entity), Is.True);
            Assert.That(entity.Transform.Parent!.Owner, Is.EqualTo(inserted));

            Assert.That(container2.Remove(entity), Is.True);
            Assert.That(container.Contains(entity), Is.True);
            Assert.That(entity.Transform.Parent!.Owner, Is.EqualTo(owner));

            owner.Delete();
            Assert.That(transform.Deleted, Is.True);
        }

        [Test]
        public void TestNestedRemovalWithDenial()
        {
            var coordinates = new EntityCoordinates(new EntityUid(1), (0, 0));
            var entityOne = EntityManager.SpawnEntity("dummy", coordinates);
            var entityTwo = EntityManager.SpawnEntity("dummy", coordinates);
            var entityThree = EntityManager.SpawnEntity("dummy", coordinates);
            var entityItem = EntityManager.SpawnEntity("dummy", coordinates);

            var container = ContainerManagerComponent.Create<Container>("dummy", entityOne);
            var container2 = ContainerManagerComponent.Create<ContainerOnlyContainer>("dummy", entityTwo);
            var container3 = ContainerManagerComponent.Create<Container>("dummy", entityThree);

            Assert.That(container.Insert(entityTwo), Is.True);
            Assert.That(entityTwo.Transform.Parent!.Owner, Is.EqualTo(entityOne));

            Assert.That(container2.Insert(entityThree), Is.True);
            Assert.That(entityThree.Transform.Parent!.Owner, Is.EqualTo(entityTwo));

            Assert.That(container3.Insert(entityItem), Is.True);
            Assert.That(entityItem.Transform.Parent!.Owner, Is.EqualTo(entityThree));

            Assert.That(container3.Remove(entityItem), Is.True);
            Assert.That(container.Contains(entityItem), Is.True);
            Assert.That(entityItem.Transform.Parent!.Owner, Is.EqualTo(entityOne));

            entityOne.Delete();
            Assert.That(entityTwo.Transform.Deleted, Is.True);
        }

        private class ContainerOnlyContainer : BaseContainer
        {
            /// <summary>
            /// The generic container class uses a list of entities
            /// </summary>
            private readonly List<IEntity> _containerList = new();

            /// <inheritdoc />
            public ContainerOnlyContainer(string id, IContainerManager manager) : base(id, manager) { }

            /// <inheritdoc />
            public override IReadOnlyList<IEntity> ContainedEntities => _containerList;

            /// <inheritdoc />
            protected override void InternalInsert(IEntity toinsert)
            {
                _containerList.Add(toinsert);
                base.InternalInsert(toinsert);
            }

            /// <inheritdoc />
            protected override void InternalRemove(IEntity toremove)
            {
                _containerList.Remove(toremove);
                base.InternalRemove(toremove);
            }

            /// <inheritdoc />
            public override bool Contains(IEntity contained)
            {
                return _containerList.Contains(contained);
            }

            /// <inheritdoc />
            public override void Shutdown()
            {
                base.Shutdown();

                foreach (var entity in _containerList)
                {
                    entity.Delete();
                }
            }

            public override bool CanInsert(IEntity toinsert)
            {
                return toinsert.TryGetComponent(out IContainerManager _);
            }
        }
    }
}
