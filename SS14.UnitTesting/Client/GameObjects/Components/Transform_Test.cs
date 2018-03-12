using System.IO;
using NUnit.Framework;
using SS14.Client.Interfaces.GameObjects;
using SS14.Server.GameObjects;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Prototypes;

namespace SS14.UnitTesting.Client.GameObjects.Components
{
    [TestFixture]
    [TestOf(typeof(TransformComponent))]
    class Transform_Test : SS14UnitTest
    {
        public override UnitTestProject Project => UnitTestProject.Client;

        private EntityManager EntityManager;
        private IMapManager MapManager;

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
            EntityManager = (EntityManager)IoCManager.Resolve<IClientEntityManager>();
            MapManager = IoCManager.Resolve<IMapManager>();

            var manager = IoCManager.Resolve<IPrototypeManager>();
            manager.LoadFromStream(new StringReader(PROTOTYPES));
            manager.Resync();

            // build the net dream
            var newMap = MapManager.CreateMap(new MapId(1));
            newMap.CreateGrid(new GridId(4));

            newMap = MapManager.CreateMap(new MapId(2));
            newMap.CreateGrid(new GridId(5));

            //NOTE: The grids have not moved, so we can assert worldpos == localpos for the tests
        }

        /// <summary>
        ///     Make sure that a child entity does not move when attaching to a parent.
        /// </summary>
        [Test]
        public void ParentAttachMoveTest()
        {
            // Arrange
            var parent = EntityManager.SpawnEntity("dummy");
            var child = EntityManager.SpawnEntity("dummy");
            var parentTrans = parent.GetComponent<ITransformComponent>();
            var childTrans = child.GetComponent<ITransformComponent>();

            var compState = new TransformComponentState(new LocalCoordinates(5, 5, new GridId(5), new MapId(2)), new Angle(0), EntityUid.Invalid);
            parentTrans.HandleComponentState(compState);

            compState = new TransformComponentState(new LocalCoordinates(6, 6, new GridId(5), new MapId(2)), new Angle(0), EntityUid.Invalid);
            childTrans.HandleComponentState(compState);

            // Act
            var oldWpos = childTrans.WorldPosition;
            compState = new TransformComponentState(new LocalCoordinates(6, 6, new GridId(5), new MapId(2)), new Angle(0), parent.Uid);
            childTrans.HandleComponentState(compState);
            var newWpos = childTrans.WorldPosition;

            // Assert
            Assert.That(oldWpos == newWpos);
        }

        /// <summary>
        ///     Tests that world rotation is built properly
        /// </summary>
        [Test]
        public void WorldRotationTest()
        {
            // Arrange
            var node1 = EntityManager.SpawnEntity("dummy");
            var node2 = EntityManager.SpawnEntity("dummy");
            var node3 = EntityManager.SpawnEntity("dummy");

            node1.Name = "node1_dummy";
            node2.Name = "node2_dummy";
            node3.Name = "node3_dummy";

            var node1Trans = node1.GetComponent<ITransformComponent>();
            var node2Trans = node2.GetComponent<ITransformComponent>();
            var node3Trans = node3.GetComponent<ITransformComponent>();

            var compState = new TransformComponentState(new LocalCoordinates(6, 6, new GridId(5), new MapId(2)), Angle.FromDegrees(135), EntityUid.Invalid);
            node1Trans.HandleComponentState(compState);
            compState = new TransformComponentState(new LocalCoordinates(7, 7, new GridId(5), new MapId(2)), Angle.FromDegrees(45), node1.Uid);
            node2Trans.HandleComponentState(compState);
            compState = new TransformComponentState(new LocalCoordinates(7, 7, new GridId(5), new MapId(2)), Angle.FromDegrees(45), node2.Uid);
            node3Trans.HandleComponentState(compState);

            // Act
            var result = node3Trans.WorldRotation;

            // Assert (135 + 45 + 45 = 225)
            Assert.That(result.EqualsApprox(Angle.FromDegrees(225)), result.Degrees.ToString);
        }
    }
}
