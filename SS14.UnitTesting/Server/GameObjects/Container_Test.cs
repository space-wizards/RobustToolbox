using NUnit.Framework;
using SS14.Server.GameObjects.Components.Container;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Prototypes;
using System.Collections.Generic;
using System.IO;

namespace SS14.UnitTesting.Server.GameObjects
{
    [TestFixture]
    public class ContainerTest : SS14UnitTest
    {
        private IServerEntityManager EntityManager;

        [OneTimeSetUp]
        public void Setup()
        {
            EntityManager = IoCManager.Resolve<IServerEntityManager>();

            var manager = IoCManager.Resolve<IPrototypeManager>();
            manager.LoadFromStream(new StringReader(PROTOTYPES));
            manager.Resync();
        }

        [Test]
        public void TestCreation()
        {
            var entity = EntityManager.SpawnEntity("dummy");

            var container = Container.Create("dummy", entity);

            Assert.That(container.ID, Is.EqualTo("dummy"));
            Assert.That(container.Owner, Is.EqualTo(entity));

            var manager = entity.GetComponent<IContainerManager>();

            Assert.That(container.Manager, Is.EqualTo(manager));
            Assert.That(() => Container.Create("dummy", entity), Throws.ArgumentException);

            Assert.That(manager.HasContainer("dummy2"), Is.False);
            var container2 = Container.Create("dummy2", entity);

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
            var owner = EntityManager.SpawnEntity("dummy");
            var inserted = EntityManager.SpawnEntity("dummy");
            var transform = inserted.GetComponent<IServerTransformComponent>();

            System.Console.WriteLine(owner.GetComponents());

            var container = Container.Create("dummy", owner);
            Assert.That(container.Insert(inserted), Is.True);
            Assert.That(transform.Parent.Owner, Is.EqualTo(owner));

            var container2 = Container.Create("dummy", inserted);
            Assert.That(() => container2.Insert(owner), Throws.InvalidOperationException);

            var success = container.Remove(inserted);
            Assert.That(success, Is.True);

            success = container.Remove(inserted);
            Assert.That(success, Is.False);

            container.Insert(inserted);
            owner.Delete();
            // Make sure inserted was detached.
            Assert.That(transform.IsMapTransform, Is.True);
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
