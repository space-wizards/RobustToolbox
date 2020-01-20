using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Robust.UnitTesting.Server.GameObjects.Components
{
    [TestFixture]
    public class ContainerTest : RobustUnitTest
    {
        private IServerEntityManager EntityManager;

        [OneTimeSetUp]
        public void Setup()
        {
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
            var entity = EntityManager.SpawnEntity("dummy", new GridCoordinates(0,0,new GridId(1)));

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
            var owner = EntityManager.SpawnEntity("dummy", new GridCoordinates(0,0,new GridId(1)));
            var inserted = EntityManager.SpawnEntity("dummy", new GridCoordinates(0,0,new GridId(1)));
            var transform = inserted.Transform;

            var container = ContainerManagerComponent.Create<Container>("dummy", owner);
            Assert.That(container.Insert(inserted), Is.True);
            Assert.That(transform.Parent.Owner, Is.EqualTo(owner));

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

        const string PROTOTYPES = @"
- type: entity
  name: dummy
  id: dummy
  components:
  - type: Transform
";
    }
}
