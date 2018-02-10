using System.IO;
using NUnit.Framework;
using SS14.Server.GameObjects;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Prototypes;

namespace SS14.UnitTesting.Server.GameObjects.Components
{
    [TestFixture]
    [TestOf(typeof(TransformComponent))]
    class Transform_Test : SS14UnitTest
    {
        private IServerEntityManager EntityManager;
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
            EntityManager = IoCManager.Resolve<IServerEntityManager>();
            MapManager = IoCManager.Resolve<IMapManager>();

            var manager = IoCManager.Resolve<IPrototypeManager>();
            manager.LoadFromStream(new StringReader(PROTOTYPES));
            manager.Resync();

            // build the net dream
            var newMap = MapManager.CreateMap(new MapId(1));
            newMap.CreateGrid(new GridId(4));

            newMap = MapManager.CreateMap(new MapId(2));
            newMap.CreateGrid(new GridId(5));

            //NOTE: The grids have not moved, so we can assert worldpos == localpos for the test
        }

        [Test]
        public void ParentMapSwitchTest()
        {
            // two entities
            var parent = EntityManager.SpawnEntity("dummy");
            var child = EntityManager.SpawnEntity("dummy");

            var parentTrans = parent.GetComponent<IServerTransformComponent>();
            var childTrans = child.GetComponent<IServerTransformComponent>();

            // that are not on the same map
            parentTrans.LocalPosition = new LocalCoordinates(5, 5, new GridId(4), new MapId(1));
            childTrans.LocalPosition = new LocalCoordinates(4, 4, new GridId(5), new MapId(2));
            
            // if they are parented, the child keeps its world position, but moves to the parents map
            childTrans.AttachParent(parentTrans);
            
            Assert.That(childTrans.MapID == parentTrans.MapID);
            Assert.That(childTrans.GridID == parentTrans.GridID);
            Assert.That(childTrans.LocalPosition == new LocalCoordinates(4, 4, new GridId(4), new MapId(1)), childTrans.LocalPosition.ToString);
            Assert.That(childTrans.WorldPosition == new Vector2(4, 4), childTrans.WorldPosition.ToString);

            // now you can move the child by setting the position, but the map/grid stays unchanged
            childTrans.LocalPosition = new LocalCoordinates(5, 5, new GridId(5), new MapId(2));

            Assert.That(childTrans.MapID == parentTrans.MapID, childTrans.MapID.ToString);
            Assert.That(childTrans.GridID == parentTrans.GridID, childTrans.GridID.ToString);
            Assert.That(childTrans.LocalPosition == parentTrans.LocalPosition, childTrans.LocalPosition.ToString);
            Assert.That(childTrans.WorldPosition == parentTrans.WorldPosition, childTrans.WorldPosition.ToString);

            // move the parent, and the child should move with it
            childTrans.WorldPosition = new Vector2(6, 6);
            parentTrans.WorldPosition += new Vector2(-7, -7);
            
            Assert.That(childTrans.WorldPosition == new Vector2(-1,-1), childTrans.WorldPosition.ToString);

            // if we detach parent, the child should be left where it was, still relative to parents grid
            var oldLpos = childTrans.LocalPosition;
            var oldWpos = childTrans.WorldPosition;

            childTrans.DetachParent();

            Assert.That(oldLpos == childTrans.LocalPosition);
            Assert.That(oldWpos == childTrans.WorldPosition);
        }

        /// <summary>
        ///     Tests that a child entity does not move when attaching to a parent.
        /// </summary>
        [Test]
        public void ParentAttachMoveTest()
        {
            // Arrange
            var parent = EntityManager.SpawnEntity("dummy");
            var child = EntityManager.SpawnEntity("dummy");
            var parentTrans = parent.GetComponent<IServerTransformComponent>();
            var childTrans = child.GetComponent<IServerTransformComponent>();
            parentTrans.WorldPosition = new Vector2(5,5);
            childTrans.WorldPosition = new Vector2(6,6);

            // Act
            var oldWpos = childTrans.WorldPosition;
            childTrans.AttachParent(parentTrans);
            var newWpos = childTrans.WorldPosition;

            // Assert
            Assert.That(oldWpos == newWpos);
        }

        /// <summary>
        ///     Tests that the entity orbits properly when the parent rotates.
        /// </summary>
        [Test]
        public void ParentRotateTest()
        {
            // Arrange
            var parent = EntityManager.SpawnEntity("dummy");
            var child = EntityManager.SpawnEntity("dummy");
            var parentTrans = parent.GetComponent<IServerTransformComponent>();
            var childTrans = child.GetComponent<IServerTransformComponent>();
            parentTrans.WorldPosition = new Vector2(0, 0);
            childTrans.WorldPosition = new Vector2(2, 0);
            childTrans.AttachParent(parentTrans);

            //Act
            parentTrans.LocalRotation = new Angle(MathHelper.Pi / 2);

            //Assert
            var result = childTrans.WorldPosition;
            Assert.That(FloatMath.CloseTo(result.X, 0), result.ToString);
            Assert.That(FloatMath.CloseTo(result.Y, 2), result.ToString);
        }

        /// <summary>
        ///     Tests that the entity orbits properly when the parent rotates and is not at the origin.
        /// </summary>
        [Test]
        public void ParentTransRotateTest()
        {
            // Arrange
            var parent = EntityManager.SpawnEntity("dummy");
            var child = EntityManager.SpawnEntity("dummy");
            var parentTrans = parent.GetComponent<IServerTransformComponent>();
            var childTrans = child.GetComponent<IServerTransformComponent>();
            parentTrans.WorldPosition = new Vector2(1, 1);
            childTrans.WorldPosition = new Vector2(2, 1);
            childTrans.AttachParent(parentTrans);

            //Act
            parentTrans.LocalRotation = new Angle(MathHelper.Pi / 2);

            //Assert
            var result = childTrans.WorldPosition;
            Assert.That(FloatMath.CloseTo(result.X, 1), result.ToString);
            Assert.That(FloatMath.CloseTo(result.Y, 2), result.ToString);
        }

        /// <summary>
        ///     Tests to see if parenting multiple entities with WorldPosition places the leaf properly.
        /// </summary>
        [Test]
        public void PositionCompositionTest()
        {
            // Arrange
            var node1 = EntityManager.SpawnEntity("dummy");
            var node2 = EntityManager.SpawnEntity("dummy");
            var node3 = EntityManager.SpawnEntity("dummy");
            var node4 = EntityManager.SpawnEntity("dummy");

            var node1Trans = node1.GetComponent<IServerTransformComponent>();
            var node2Trans = node2.GetComponent<IServerTransformComponent>();
            var node3Trans = node3.GetComponent<IServerTransformComponent>();
            var node4Trans = node4.GetComponent<IServerTransformComponent>();

            node1Trans.WorldPosition = new Vector2(0, 0);
            node2Trans.WorldPosition = new Vector2(1, 1);
            node3Trans.WorldPosition = new Vector2(2, 2);
            node4Trans.WorldPosition = new Vector2(0, 2);

            node2Trans.AttachParent(node1Trans);
            node3Trans.AttachParent(node2Trans);
            node4Trans.AttachParent(node3Trans);

            //Act
            node1Trans.LocalRotation = new Angle(MathHelper.Pi / 2);

            //Assert
            var result = node4Trans.WorldPosition;
            Assert.That(FloatMath.CloseTo(result.X, -2), result.ToString);
            Assert.That(FloatMath.CloseTo(result.Y, 0), result.ToString);
        }

        /// <summary>
        ///     Tests to see if setting the world position of a child causes position rounding errors.
        /// </summary>
        [Test]
        public void ParentWorldPositionRoundingErrorTest()
        {
            // Arrange
            var node1 = EntityManager.SpawnEntity("dummy");
            var node2 = EntityManager.SpawnEntity("dummy");
            var node3 = EntityManager.SpawnEntity("dummy");

            var node1Trans = node1.GetComponent<IServerTransformComponent>();
            var node2Trans = node2.GetComponent<IServerTransformComponent>();
            var node3Trans = node3.GetComponent<IServerTransformComponent>();

            node1Trans.WorldPosition = new Vector2(0, 0);
            node2Trans.WorldPosition = new Vector2(1, 1);
            node3Trans.WorldPosition = new Vector2(2, 2);

            node2Trans.AttachParent(node1Trans);
            node3Trans.AttachParent(node2Trans);

            // Act
            var oldWpos = node3Trans.WorldPosition;

            for (var i = 0; i < 10000; i++)
            {
                var dx = i % 2 == 0 ? 5 : -5;
                node1Trans.WorldPosition += new Vector2(dx, dx);
                node2Trans.WorldPosition += new Vector2(dx, dx);
                node3Trans.WorldPosition += new Vector2(dx, dx);
            }

            var newWpos = node3Trans.WorldPosition;

            // Assert
            Assert.That(FloatMath.CloseTo(oldWpos.X, newWpos.Y), newWpos.ToString);
            Assert.That(FloatMath.CloseTo(oldWpos.Y, newWpos.Y), newWpos.ToString);
        }

        /// <summary>
        ///     Tests to see if rotating a parent causes major child position rounding errors.
        /// </summary>
        [Test]
        public void ParentRotationRoundingErrorTest()
        {
            // Arrange
            var node1 = EntityManager.SpawnEntity("dummy");
            var node2 = EntityManager.SpawnEntity("dummy");
            var node3 = EntityManager.SpawnEntity("dummy");

            var node1Trans = node1.GetComponent<IServerTransformComponent>();
            var node2Trans = node2.GetComponent<IServerTransformComponent>();
            var node3Trans = node3.GetComponent<IServerTransformComponent>();

            node1Trans.WorldPosition = new Vector2(0, 0);
            node2Trans.WorldPosition = new Vector2(1, 1);
            node3Trans.WorldPosition = new Vector2(2, 2);

            node2Trans.AttachParent(node1Trans);
            node3Trans.AttachParent(node2Trans);

            // Act
            var oldWpos = node3Trans.WorldPosition;
            
            for (var i = 0; i < 100; i++)
            {
                node1Trans.LocalRotation += new Angle(MathHelper.Pi);
                node2Trans.LocalRotation += new Angle(MathHelper.Pi);
                node3Trans.LocalRotation += new Angle(MathHelper.Pi);
            }

            var newWpos = node3Trans.WorldPosition;

            //NOTE: Yes, this does cause a non-zero error

            // Assert
            Assert.That(FloatMath.CloseTo(oldWpos.X, newWpos.Y), newWpos.ToString);
            Assert.That(FloatMath.CloseTo(oldWpos.Y, newWpos.Y), newWpos.ToString);
        }

        /// <summary>
        ///     Tests that the world and inverse world transforms are built properly.
        /// </summary>
        [Test]
        public void TreeComposeWorldMatricesTest()
        {
            // Arrange
            const float epsilon = 1.0E-6f;
            var control = Matrix3.Identity;

            var node1 = EntityManager.SpawnEntity("dummy");
            var node2 = EntityManager.SpawnEntity("dummy");
            var node3 = EntityManager.SpawnEntity("dummy");
            var node4 = EntityManager.SpawnEntity("dummy");

            var node1Trans = node1.GetComponent<IServerTransformComponent>();
            var node2Trans = node2.GetComponent<IServerTransformComponent>();
            var node3Trans = node3.GetComponent<IServerTransformComponent>();
            var node4Trans = node4.GetComponent<IServerTransformComponent>();

            node1Trans.WorldPosition = new Vector2(0, 0);
            node2Trans.WorldPosition = new Vector2(1, 1);
            node3Trans.WorldPosition = new Vector2(2, 2);
            node4Trans.WorldPosition = new Vector2(0, 2);

            node2Trans.AttachParent(node1Trans);
            node3Trans.AttachParent(node2Trans);
            node4Trans.AttachParent(node3Trans);

            //Act
            node1Trans.LocalRotation = new Angle(MathHelper.Pi / 6.37);
            node1Trans.WorldPosition = new Vector2(1, 1);

            var worldMat = node4Trans.WorldMatrix;
            var invWorldMat = node4Trans.InvWorldMatrix;
            
            Matrix3.Multiply(ref worldMat, ref invWorldMat, out var leftVerifyMatrix);
            Matrix3.Multiply(ref invWorldMat, ref worldMat, out var rightVerifyMatrix);

            //Assert

            // these should be the same (A × A-1 = A-1 × A = I)
            Assert.That(Matrix3.EqualsApprox(ref leftVerifyMatrix, ref rightVerifyMatrix, epsilon));

            // verify matrix == identity matrix (or very close to because float precision)
            Assert.That(Matrix3.EqualsApprox(ref leftVerifyMatrix, ref control, epsilon), leftVerifyMatrix.ToString);
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

            var node1Trans = node1.GetComponent<IServerTransformComponent>();
            var node2Trans = node2.GetComponent<IServerTransformComponent>();
            var node3Trans = node3.GetComponent<IServerTransformComponent>();
;
            node2Trans.AttachParent(node1Trans);
            node3Trans.AttachParent(node2Trans);

            node1Trans.LocalRotation = Angle.FromDegrees(0);
            node2Trans.LocalRotation = Angle.FromDegrees(45);
            node3Trans.LocalRotation = Angle.FromDegrees(45);

            // Act
            node1Trans.LocalRotation = Angle.FromDegrees(135);

            // Assert (135 + 45 + 45 = 225)
            var result = node3Trans.WorldRotation;
            Assert.That(result.EqualsApprox(Angle.FromDegrees(225)), result.Degrees.ToString);
        }
    }
}
