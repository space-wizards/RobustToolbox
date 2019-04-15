using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SS14.Client.Interfaces.GameObjects;
using SS14.Server.GameObjects;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Prototypes;

namespace SS14.UnitTesting.Server.GameObjects
{
    [TestFixture]
    [TestOf(typeof(ServerEntityManager))]
    class ServerEntityManager_Test : SS14UnitTest
    {
        public override UnitTestProject Project => UnitTestProject.Server;

        private IServerEntityManager EntityManager;

        const string PROTOTYPES = @"
- type: entity
  name: dummyPoint
  id: dummyPoint
  components:
  - type: Transform

- type: entity
  name: dummyAABB
  id: dummyAABB
  components:
  - type: Transform
  - type: BoundingBox
";

        [OneTimeSetUp]
        public void Setup()
        {
            EntityManager = IoCManager.Resolve<IServerEntityManager>();

            var manager = IoCManager.Resolve<IPrototypeManager>();
            manager.LoadFromStream(new StringReader(PROTOTYPES));
            manager.Resync();
        }

        [Test]
        public void GetEntityInRangePointTest()
        {
            // Arrange
            var baseEnt = EntityManager.SpawnEntity("dummyPoint");
            var inRangeEnt = EntityManager.SpawnEntity("dummyPoint");
            inRangeEnt.GetComponent<IServerTransformComponent>().WorldPosition = new Vector2(-2, -2);

            // Act
            var results = EntityManager.GetEntitiesInRange(baseEnt, 4.00f);

            // Cleanup
            var list = results.ToList();
            EntityManager.FlushEntities();

            // Assert
            Assert.That(list.Count, Is.EqualTo(2), list.Count.ToString);
        }
        
        [Test]
        public void GetEntityInRangeAABBTest()
        {
            // Arrange
            var baseEnt = EntityManager.SpawnEntity("dummyAABB");
            var inRangeEnt = EntityManager.SpawnEntity("dummyAABB");
            inRangeEnt.GetComponent<IServerTransformComponent>().WorldPosition = new Vector2(-2, -2);

            // Act
            var results = EntityManager.GetEntitiesInRange(baseEnt, 4.00f);

            // Cleanup
            var list = results.ToList();
            EntityManager.FlushEntities();

            // Assert
            Assert.That(list.Count, Is.EqualTo(2), list.Count.ToString);
        }
    }
}
