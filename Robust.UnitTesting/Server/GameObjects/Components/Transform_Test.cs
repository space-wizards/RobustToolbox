using System.IO;
using System.Numerics;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Timing;

namespace Robust.UnitTesting.Server.GameObjects.Components
{
    [TestFixture]
    [TestOf(typeof(TransformComponent))]
    sealed class Transform_Test : RobustUnitTest
    {
        public override UnitTestProject Project => UnitTestProject.Server;

        private IEntityManager EntityManager = default!;
        private IMapManager MapManager = default!;
        private SharedTransformSystem XformSystem => EntityManager.System<SharedTransformSystem>();

        const string Prototypes = @"
- type: entity
  name: dummy
  id: mapDummy
  components:
  - type: Transform
  - type: Map
    index: 123
  # Due to the map getting initialised last this seemed easiest to fix the test while removing the mocks.
  - type: Broadphase
";

        private MapId MapA;
        private Entity<MapGridComponent> GridA;
        private MapId MapB;
        private Entity<MapGridComponent> GridB;

        private static readonly EntityCoordinates InitialPos = new(EntityUid.FirstUid, new Vector2(0, 0));

        [OneTimeSetUp]
        public void Setup()
        {
            IoCManager.Resolve<IComponentFactory>().GenerateNetIds();

            EntityManager = IoCManager.Resolve<IEntityManager>();
            MapManager = IoCManager.Resolve<IMapManager>();

            IoCManager.Resolve<ISerializationManager>().Initialize();
            var manager = IoCManager.Resolve<IPrototypeManager>();
            manager.RegisterKind(typeof(EntityPrototype), typeof(EntityCategoryPrototype));
            manager.LoadFromStream(new StringReader(Prototypes));
            manager.ResolveResults();

            var mapSys = EntityManager.System<SharedMapSystem>();
            // build the net dream
            mapSys.CreateMap(out MapA);
            mapSys.CreateMap(out MapB);

            GridA = MapManager.CreateGridEntity(MapA);
            GridB = MapManager.CreateGridEntity(MapB);

            //NOTE: The grids have not moved, so we can assert worldpos == localpos for the test
        }

        [SetUp]
        public void ClearSimulation()
        {
            // One of the tests changes this so we use this to ensure it doesn't get passed to other tests.
            IoCManager.Resolve<IGameTiming>().InSimulation = false;
        }

        [Test]
        public void ParentMapSwitchTest()
        {
            // two entities
            var parent = EntityManager.SpawnEntity(null, InitialPos);
            var child = EntityManager.SpawnEntity(null, InitialPos);

            var parentTrans = EntityManager.GetComponent<TransformComponent>(parent);
            var childTrans = EntityManager.GetComponent<TransformComponent>(child);

            // that are not on the same map
            XformSystem.SetCoordinates(parent, parentTrans, new EntityCoordinates(GridA, new Vector2(5, 5)));
            XformSystem.SetCoordinates(child, childTrans, new EntityCoordinates(GridB, new Vector2(4, 4)));

            // if they are parented, the child keeps its world position, but moves to the parents map
            XformSystem.SetParent(child, childTrans, parent, parentXform: parentTrans);

            Assert.Multiple(() =>
            {
                Assert.That(childTrans.MapID, NUnit.Framework.Is.EqualTo(parentTrans.MapID));
                Assert.That(childTrans.GridUid, NUnit.Framework.Is.EqualTo(parentTrans.GridUid));
                Assert.That(childTrans.Coordinates, NUnit.Framework.Is.EqualTo(new EntityCoordinates(parent, new Vector2(-1, -1))));
                Assert.That(XformSystem.GetWorldPosition(childTrans), NUnit.Framework.Is.EqualTo(new Vector2(4, 4)));
            });

            // move the parent, and the child should move with it
            XformSystem.SetLocalPosition(child, new Vector2(6, 6), childTrans);
            XformSystem.SetWorldPosition(parent, new Vector2(-8, -8));

            Assert.That(XformSystem.GetWorldPosition(childTrans), NUnit.Framework.Is.EqualTo(new Vector2(-2, -2)));

            // if we detach parent, the child should be left where it was, still relative to parents grid
            var oldLpos = new Vector2(-2, -2);
            var oldWpos = XformSystem.GetWorldPosition(childTrans);

            XformSystem.AttachToGridOrMap(child, childTrans);

            // the gridId won't match, because we just detached from the grid entity

            Assert.Multiple(() =>
            {
                Assert.That(childTrans.Coordinates.Position, NUnit.Framework.Is.EqualTo(oldLpos));
                Assert.That(XformSystem.GetWorldPosition(childTrans), NUnit.Framework.Is.EqualTo(oldWpos));
            });
        }

        /// <summary>
        ///     Tests that a child entity does not move when attaching to a parent.
        /// </summary>
        [Test]
        public void ParentAttachMoveTest()
        {
            // Arrange
            var parent = EntityManager.SpawnEntity(null, InitialPos);
            var child = EntityManager.SpawnEntity(null, InitialPos);
            var parentTrans = EntityManager.GetComponent<TransformComponent>(parent);
            var childTrans = EntityManager.GetComponent<TransformComponent>(child);
            XformSystem.SetWorldPosition(parent, new Vector2(5, 5));
            XformSystem.SetWorldPosition(child, new Vector2(6, 6));

            // Act
            var oldWpos = XformSystem.GetWorldPosition(childTrans);
            XformSystem.SetParent(child, childTrans, parent, parentXform: parentTrans);
            var newWpos = XformSystem.GetWorldPosition(childTrans);

            // Assert
            Assert.That(oldWpos, NUnit.Framework.Is.EqualTo(newWpos));
        }

        /// <summary>
        ///     Tests that a child entity does not move when attaching to a parent.
        /// </summary>
        [Test]
        public void ParentDoubleAttachMoveTest()
        {
            // Arrange
            var parent = EntityManager.SpawnEntity(null, InitialPos);
            var childOne = EntityManager.SpawnEntity(null, InitialPos);
            var childTwo = EntityManager.SpawnEntity(null, InitialPos);
            var parentTrans = EntityManager.GetComponent<TransformComponent>(parent);
            var childOneTrans = EntityManager.GetComponent<TransformComponent>(childOne);
            var childTwoTrans = EntityManager.GetComponent<TransformComponent>(childTwo);
            XformSystem.SetWorldPosition(parent, new Vector2(1, 1));
            XformSystem.SetWorldPosition(childOne, new Vector2(2, 2));
            XformSystem.SetWorldPosition(childTwo, new Vector2(3, 3));

            // Act
            var oldWpos = XformSystem.GetWorldPosition(childOneTrans);
            XformSystem.SetParent(childOne, childOneTrans, parent, parentXform: parentTrans);
            var newWpos = XformSystem.GetWorldPosition(childOneTrans);
            Assert.That(oldWpos, NUnit.Framework.Is.EqualTo(newWpos));

            oldWpos = XformSystem.GetWorldPosition(childTwoTrans);
            XformSystem.SetParent(childOne, childOneTrans, parent, parentXform: parentTrans);
            newWpos = XformSystem.GetWorldPosition(childTwoTrans);
            Assert.That(oldWpos, NUnit.Framework.Is.EqualTo(newWpos));

            oldWpos = XformSystem.GetWorldPosition(childTwoTrans);
            XformSystem.SetParent(childTwo, childTwoTrans, childOne, parentXform: childOneTrans);
            newWpos = XformSystem.GetWorldPosition(childTwoTrans);
            Assert.That(oldWpos, NUnit.Framework.Is.EqualTo(newWpos));
        }

        /// <summary>
        ///     Tests that the entity orbits properly when the parent rotates.
        /// </summary>
        [Test]
        public void ParentRotateTest()
        {
            // Arrange
            var parent = EntityManager.SpawnEntity(null, InitialPos);
            var child = EntityManager.SpawnEntity(null, InitialPos);
            var parentTrans = EntityManager.GetComponent<TransformComponent>(parent);
            var childTrans = EntityManager.GetComponent<TransformComponent>(child);
            XformSystem.SetWorldPosition(parent, new Vector2(0, 0));
            XformSystem.SetWorldPosition(child, new Vector2(2, 0));
            XformSystem.SetParent(child, childTrans, parent, parentXform: parentTrans);

            //Act
            parentTrans.LocalRotation = new Angle(MathHelper.Pi / 2);

            //Assert
            var result = XformSystem.GetWorldPosition(childTrans);
            Assert.Multiple(() =>
            {
                Assert.That(MathHelper.CloseToPercent(result.X, 0));
                Assert.That(MathHelper.CloseToPercent(result.Y, 2));
            });
        }

        /// <summary>
        ///     Tests that the entity orbits properly when the parent rotates and is not at the origin.
        /// </summary>
        [Test]
        public void ParentTransRotateTest()
        {
            // Arrange
            var parent = EntityManager.SpawnEntity(null, InitialPos);
            var child = EntityManager.SpawnEntity(null, InitialPos);
            var parentTrans = EntityManager.GetComponent<TransformComponent>(parent);
            var childTrans = EntityManager.GetComponent<TransformComponent>(child);
            XformSystem.SetWorldPosition(parent, new Vector2(1, 1));
            XformSystem.SetWorldPosition(child, new Vector2(2, 1));
            XformSystem.SetParent(child, childTrans, parent, parentXform: parentTrans);

            //Act
            parentTrans.LocalRotation = new Angle(MathHelper.Pi / 2);

            //Assert
            var result = XformSystem.GetWorldPosition(childTrans);
            Assert.Multiple(() =>
            {
                Assert.That(MathHelper.CloseToPercent(result.X, 1));
                Assert.That(MathHelper.CloseToPercent(result.Y, 2));
            });
        }

        /// <summary>
        ///     Tests to see if parenting multiple entities with WorldPosition places the leaf properly.
        /// </summary>
        [Test]
        public void PositionCompositionTest()
        {
            // Arrange
            var node1 = EntityManager.SpawnEntity(null, InitialPos);
            var node2 = EntityManager.SpawnEntity(null, InitialPos);
            var node3 = EntityManager.SpawnEntity(null, InitialPos);
            var node4 = EntityManager.SpawnEntity(null, InitialPos);

            var node1Trans = EntityManager.GetComponent<TransformComponent>(node1);
            var node2Trans = EntityManager.GetComponent<TransformComponent>(node2);
            var node3Trans = EntityManager.GetComponent<TransformComponent>(node3);
            var node4Trans = EntityManager.GetComponent<TransformComponent>(node4);

            XformSystem.SetWorldPosition(node1, new Vector2(0, 0));
            XformSystem.SetWorldPosition(node2, new Vector2(1, 1));
            XformSystem.SetWorldPosition(node3, new Vector2(2, 2));
            XformSystem.SetWorldPosition(node4, new Vector2(0, 2));

            XformSystem.SetParent(node2, node2Trans, node1, parentXform: node1Trans);
            XformSystem.SetParent(node3, node3Trans, node2, parentXform: node2Trans);
            XformSystem.SetParent(node4, node4Trans, node3, parentXform: node3Trans);

            //Act
            node1Trans.LocalRotation = new Angle(MathHelper.Pi / 2);

            //Assert
            var result = XformSystem.GetWorldPosition(node4Trans);

            Assert.Multiple(() =>
            {
                Assert.That(result.X, new ApproxEqualityConstraint(-2f));
                Assert.That(result.Y, new ApproxEqualityConstraint(0f));
            });
        }

        /// <summary>
        ///     Tests to see if setting the world position of a child causes position rounding errors.
        /// </summary>
        [Test]
        public void ParentLocalPositionRoundingErrorTest()
        {
            // Arrange
            var node1 = EntityManager.SpawnEntity(null, InitialPos);
            var node2 = EntityManager.SpawnEntity(null, InitialPos);
            var node3 = EntityManager.SpawnEntity(null, InitialPos);

            var node1Trans = EntityManager.GetComponent<TransformComponent>(node1);
            var node2Trans = EntityManager.GetComponent<TransformComponent>(node2);
            var node3Trans = EntityManager.GetComponent<TransformComponent>(node3);

            XformSystem.SetWorldPosition(node1, new Vector2(0, 0));
            XformSystem.SetWorldPosition(node2, new Vector2(1, 1));
            XformSystem.SetWorldPosition(node3, new Vector2(2, 2));

            XformSystem.SetParent(node1, node1Trans, node2, parentXform: node2Trans);
            XformSystem.SetParent(node2, node2Trans, node3, parentXform: node3Trans);

            // Act
            var oldWpos = XformSystem.GetWorldPosition(node3Trans);

            for (var i = 0; i < 10000; i++)
            {
                var dx = i % 2 == 0 ? 5 : -5;
                XformSystem.SetLocalPosition(node1, node1Trans.LocalPosition + new Vector2(dx, dx), node1Trans);
                XformSystem.SetLocalPosition(node2, node2Trans.LocalPosition + new Vector2(dx, dx), node2Trans);
                XformSystem.SetLocalPosition(node3, node3Trans.LocalPosition + new Vector2(dx, dx), node3Trans);
            }

            var newWpos = XformSystem.GetWorldPosition(node3Trans);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(MathHelper.CloseToPercent(oldWpos.X, newWpos.Y), $"{oldWpos.X} should be {newWpos.Y}");
                Assert.That(MathHelper.CloseToPercent(oldWpos.Y, newWpos.Y), newWpos.ToString);
            });
        }

        /// <summary>
        ///     Tests to see if rotating a parent causes major child position rounding errors.
        /// </summary>
        [Test]
        public void ParentRotationRoundingErrorTest()
        {
            IoCManager.Resolve<IGameTiming>().InSimulation = true;

            // Arrange
            var node1 = EntityManager.SpawnEntity(null, InitialPos);
            var node2 = EntityManager.SpawnEntity(null, InitialPos);
            var node3 = EntityManager.SpawnEntity(null, InitialPos);

            var node1Trans = EntityManager.GetComponent<TransformComponent>(node1);
            var node2Trans = EntityManager.GetComponent<TransformComponent>(node2);
            var node3Trans = EntityManager.GetComponent<TransformComponent>(node3);

            XformSystem.SetWorldPosition(node1, new Vector2(0, 0));
            XformSystem.SetWorldPosition(node2, new Vector2(1, 1));
            XformSystem.SetWorldPosition(node3, new Vector2(2, 2));

            XformSystem.SetParent(node2, node2Trans, node1, parentXform: node1Trans);
            XformSystem.SetParent(node3, node3Trans, node2, parentXform: node2Trans);

            // Act
            var oldWpos = XformSystem.GetWorldPosition(node3Trans);

            for (var i = 0; i < 100; i++)
            {
                node1Trans.LocalRotation += new Angle(MathHelper.Pi);
                node2Trans.LocalRotation += new Angle(MathHelper.Pi);
                node3Trans.LocalRotation += new Angle(MathHelper.Pi);
            }

            var newWpos = XformSystem.GetWorldPosition(node3Trans);

            //NOTE: Yes, this does cause a non-zero error

            // Assert

            Assert.Multiple(() =>
            {
                Assert.That(MathHelper.CloseToPercent(oldWpos.X, newWpos.Y, 0.0001f));
                Assert.That(MathHelper.CloseToPercent(oldWpos.Y, newWpos.Y, 0.0001f));
            });
        }

        /// <summary>
        ///     Tests that the world and inverse world transforms are built properly.
        /// </summary>
        [Test]
        public void TreeComposeWorldMatricesTest()
        {
            // Arrange
            var control = Matrix3x2.Identity;

            var node1 = EntityManager.SpawnEntity(null, InitialPos);
            var node2 = EntityManager.SpawnEntity(null, InitialPos);
            var node3 = EntityManager.SpawnEntity(null, InitialPos);
            var node4 = EntityManager.SpawnEntity(null, InitialPos);

            var node1Trans = EntityManager.GetComponent<TransformComponent>(node1);
            var node2Trans = EntityManager.GetComponent<TransformComponent>(node2);
            var node3Trans = EntityManager.GetComponent<TransformComponent>(node3);
            var node4Trans = EntityManager.GetComponent<TransformComponent>(node4);

            XformSystem.SetWorldPosition(node1, new Vector2(0, 0));
            XformSystem.SetWorldPosition(node2, new Vector2(1, 1));
            XformSystem.SetWorldPosition(node3, new Vector2(2, 2));
            XformSystem.SetWorldPosition(node4, new Vector2(0, 2));

            XformSystem.SetParent(node2, node2Trans, node1, parentXform: node1Trans);
            XformSystem.SetParent(node3, node3Trans, node2, parentXform: node2Trans);
            XformSystem.SetParent(node4, node4Trans, node3, parentXform: node3Trans);

            //Act
            node1Trans.LocalRotation = new Angle(MathHelper.Pi / 6.37);
            XformSystem.SetWorldPosition(node1, new Vector2(1, 1));

            var worldMat = XformSystem.GetWorldMatrix(node4Trans);
            var invWorldMat = XformSystem.GetInvWorldMatrix(node4Trans);

            var leftVerifyMatrix = Matrix3x2.Multiply(worldMat, invWorldMat);
            var rightVerifyMatrix = Matrix3x2.Multiply(invWorldMat, worldMat);

            //Assert

            Assert.Multiple(() =>
            {
                // these should be the same (A × A-1 = A-1 × A = I)
                Assert.That(leftVerifyMatrix, new ApproxEqualityConstraint(rightVerifyMatrix));

                // verify matrix == identity matrix (or very close to because float precision)
                Assert.That(leftVerifyMatrix, new ApproxEqualityConstraint(control));
            });
        }

        /// <summary>
        ///     Tests that world rotation is built properly
        /// </summary>
        [Test]
        public void WorldRotationTest()
        {
            // Arrange
            var node1 = EntityManager.SpawnEntity(null, InitialPos);
            var node2 = EntityManager.SpawnEntity(null, InitialPos);
            var node3 = EntityManager.SpawnEntity(null, InitialPos);

            var node1Trans = EntityManager.GetComponent<TransformComponent>(node1);
            var node2Trans = EntityManager.GetComponent<TransformComponent>(node2);
            var node3Trans = EntityManager.GetComponent<TransformComponent>(node3);

            XformSystem.SetParent(node2, node2Trans, node1, parentXform: node1Trans);
            XformSystem.SetParent(node3, node3Trans, node2, parentXform: node2Trans);

            node1Trans.LocalRotation = Angle.FromDegrees(0);
            node2Trans.LocalRotation = Angle.FromDegrees(45);
            node3Trans.LocalRotation = Angle.FromDegrees(45);

            // Act
            node1Trans.LocalRotation = Angle.FromDegrees(135);

            // Assert (135 + 45 + 45 = 225)
            var result = XformSystem.GetWorldRotation(node3Trans);
            Assert.That(result, new ApproxEqualityConstraint(Angle.FromDegrees(225)));
        }

        /// <summary>
        ///     Test that, in a chain A -> B -> C, if A is moved C's world position correctly updates.
        /// </summary>
        [Test]
        public void MatrixUpdateTest()
        {
            var node1 = EntityManager.SpawnEntity(null, InitialPos);
            var node2 = EntityManager.SpawnEntity(null, InitialPos);
            var node3 = EntityManager.SpawnEntity(null, InitialPos);

            var node1Trans = EntityManager.GetComponent<TransformComponent>(node1);
            var node2Trans = EntityManager.GetComponent<TransformComponent>(node2);
            var node3Trans = EntityManager.GetComponent<TransformComponent>(node3);

            XformSystem.SetParent(node2, node2Trans, node1, parentXform: node1Trans);
            XformSystem.SetParent(node3, node3Trans, node2, parentXform: node2Trans);

            XformSystem.SetLocalPosition(node3, new Vector2(5, 5), node3Trans);
            XformSystem.SetLocalPosition(node2, new Vector2(5, 5), node2Trans);
            XformSystem.SetLocalPosition(node1, new Vector2(5, 5), node1Trans);

            Assert.That(XformSystem.GetWorldPosition(node3Trans), new ApproxEqualityConstraint(new Vector2(15, 15)));
        }

        /*
         * There used to be a TestMapInitOrder test here. The problem is that the actual game will probably explode if
         * you start initialising children before parents and the test only worked because of specific setup being done
         * to prevent this in its use case.
         */
    }
}
