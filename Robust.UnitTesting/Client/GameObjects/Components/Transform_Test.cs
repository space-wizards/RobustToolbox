using System.IO;
using Moq;
using NUnit.Framework;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Robust.UnitTesting.Client.GameObjects.Components
{
    [TestFixture]
    [TestOf(typeof(TransformComponent))]
    class Transform_Test : RobustUnitTest
    {
        public override UnitTestProject Project => UnitTestProject.Client;

        private IClientEntityManager EntityManager = default!;
        private IMapManager MapManager = default!;

        const string PROTOTYPES = @"
- type: entity
  name: dummy
  id: dummy
  components:
  - type: Transform
";

        private MapId MapA;
        private IMapGrid GridA = default!;
        private MapId MapB;
        private IMapGrid GridB = default!;

        protected override void OverrideIoC()
        {
            base.OverrideIoC();

            var mock = new Mock<IEntitySystemManager>();
            var system = new SharedTransformSystem();
            mock.Setup(m => m.GetEntitySystem<SharedTransformSystem>()).Returns(system);

            IoCManager.RegisterInstance<IEntitySystemManager>(mock.Object, true);
        }

        [OneTimeSetUp]
        public void Setup()
        {
            var compMan = IoCManager.Resolve<IComponentManager>();
            compMan.Initialize();
            EntityManager = IoCManager.Resolve<IClientEntityManager>();
            MapManager = IoCManager.Resolve<IMapManager>();
            MapManager.Initialize();
            MapManager.Startup();

            IoCManager.Resolve<IServ3Manager>().Initialize();
            var manager = IoCManager.Resolve<IPrototypeManager>();
            manager.LoadFromStream(new StringReader(PROTOTYPES));
            manager.Resync();

            // build the net dream
            MapA = MapManager.CreateMap();
            GridA = MapManager.CreateGrid(MapA);

            MapB = MapManager.CreateMap();
            GridB = MapManager.CreateGrid(MapB);

            //NOTE: The grids have not moved, so we can assert worldpos == localpos for the tests
        }

        /// <summary>
        ///     Make sure that component state locations are RELATIVE.
        /// </summary>
        [Test]
        public void ComponentStatePositionTest()
        {
            // Arrange
            var initialPos = new EntityCoordinates(GridA.GridEntityId, (0, 0));
            var parent = EntityManager.SpawnEntity("dummy", initialPos);
            var child = EntityManager.SpawnEntity("dummy", initialPos);
            var parentTrans = parent.Transform;
            var childTrans = child.Transform;

            var compState = new TransformComponent.TransformComponentState(new Vector2(5, 5), new Angle(0), GridB.GridEntityId, false);
            parentTrans.HandleComponentState(compState, null);

            compState = new TransformComponent.TransformComponentState(new Vector2(6, 6), new Angle(0), GridB.GridEntityId, false);
            childTrans.HandleComponentState(compState, null);
            // World pos should be 6, 6 now.

            // Act
            var oldWpos = childTrans.WorldPosition;
            compState = new TransformComponent.TransformComponentState(new Vector2(1, 1), new Angle(0), parent.Uid, false);
            childTrans.HandleComponentState(compState, null);
            var newWpos = childTrans.WorldPosition;

            // Assert
            Assert.That(newWpos, Is.EqualTo(oldWpos));
        }

        /// <summary>
        ///     Tests that world rotation is built properly
        /// </summary>
        [Test]
        public void WorldRotationTest()
        {
            // Arrange
            var initalPos = new EntityCoordinates(GridA.GridEntityId, (0, 0));
            var node1 = EntityManager.SpawnEntity("dummy", initalPos);
            var node2 = EntityManager.SpawnEntity("dummy", initalPos);
            var node3 = EntityManager.SpawnEntity("dummy", initalPos);

            node1.Name = "node1_dummy";
            node2.Name = "node2_dummy";
            node3.Name = "node3_dummy";

            var node1Trans = node1.Transform;
            var node2Trans = node2.Transform;
            var node3Trans = node3.Transform;

            var compState = new TransformComponent.TransformComponentState(new Vector2(6, 6), Angle.FromDegrees(135), GridB.GridEntityId, false);
            node1Trans.HandleComponentState(compState, null);
            compState = new TransformComponent.TransformComponentState(new Vector2(1, 1), Angle.FromDegrees(45), node1.Uid, false);
            node2Trans.HandleComponentState(compState, null);
            compState = new TransformComponent.TransformComponentState(new Vector2(0, 0), Angle.FromDegrees(45), node2.Uid, false);
            node3Trans.HandleComponentState(compState, null);

            // Act
            var result = node3Trans.WorldRotation;

            // Assert (135 + 45 + 45 = 225)
            Assert.That(result, new ApproxEqualityConstraint(Angle.FromDegrees(225)));
        }
    }
}
