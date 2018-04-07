using System.IO;
using NUnit.Framework;
using SS14.Client.GameObjects;
using SS14.Client.Interfaces.GameObjects;
using SS14.Server.GameObjects;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
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
            var factory = IoCManager.Resolve<IComponentFactory>();
            factory.Register<ClientTransformComponent>();
            factory.RegisterReference<ClientTransformComponent, ITransformComponent>();

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
        ///
        ///     NOTE: Looking over it after making messages work differently, I'm pretty sure this test's fucked and doesn't work like it's described.
        ///     Problem is that now the messages carry relative position, which seems to be what this test was testing,
        ///         so it's pretty much testing its own input data (if that makes any sense).
        ///     Gonna leave it in since it might detect some position calculation shenanigans, but you're warned.
        /// </summary>
        [Test]
        public void ParentAttachMoveTest()
        {
            // Arrange
            var parent = EntityManager.SpawnEntity("dummy");
            var child = EntityManager.SpawnEntity("dummy");
            var parentTrans = parent.GetComponent<ITransformComponent>();
            var childTrans = child.GetComponent<ITransformComponent>();

            var compState = new TransformComponentState(new Vector2(5, 5), new GridId(5), new MapId(2), new Angle(0), EntityUid.Invalid);
            parentTrans.HandleComponentState(compState);

            compState = new TransformComponentState(new Vector2(6, 6), new GridId(5), new MapId(2), new Angle(0), EntityUid.Invalid);
            childTrans.HandleComponentState(compState);
            // World pos should be 6, 6 now.

            // Act
            var oldWpos = childTrans.WorldPosition;
            compState = new TransformComponentState(Vector2.One, new GridId(5), new MapId(2), new Angle(0), parent.Uid);
            childTrans.HandleComponentState(compState);
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
            var node1 = EntityManager.SpawnEntity("dummy");
            var node2 = EntityManager.SpawnEntity("dummy");
            var node3 = EntityManager.SpawnEntity("dummy");

            node1.Name = "node1_dummy";
            node2.Name = "node2_dummy";
            node3.Name = "node3_dummy";

            var node1Trans = node1.GetComponent<ITransformComponent>();
            var node2Trans = node2.GetComponent<ITransformComponent>();
            var node3Trans = node3.GetComponent<ITransformComponent>();

            var compState = new TransformComponentState(new Vector2(6, 6), new GridId(5), new MapId(2), Angle.FromDegrees(135), EntityUid.Invalid);
            node1Trans.HandleComponentState(compState);
            compState = new TransformComponentState(new Vector2(1, 1), new GridId(5), new MapId(2), Angle.FromDegrees(45), node1.Uid);
            node2Trans.HandleComponentState(compState);
            compState = new TransformComponentState(Vector2.Zero, new GridId(5), new MapId(2), Angle.FromDegrees(45), node2.Uid);
            node3Trans.HandleComponentState(compState);

            // Act
            var result = node3Trans.WorldRotation;

            // Assert (135 + 45 + 45 = 225)
            Assert.That(result.EqualsApprox(Angle.FromDegrees(225)), result.Degrees.ToString);
        }
    }
}
