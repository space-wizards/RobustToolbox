using NUnit.Framework;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Client.GameObjects.Components
{
    [TestFixture]
    [TestOf(typeof(TransformComponent))]
    public class TransformComponentTests
    {
        private static readonly MapId TestMapId = new(1);
        private static readonly GridId TestGridAId = new(1);
        private static readonly GridId TestGridBId = new(2);

        private static ISimulation SimulationFactory()
        {
            var sim = RobustServerSimulation
                .NewSimulation()
                .RegisterEntitySystems(factory =>
                {
                    factory.LoadExtraSystemType<SharedTransformSystem>();
                })
                .RegisterComponents(factory =>
                {
                    factory.RegisterClass<ContainerManagerComponent>();
                    factory.RegisterClass<EntityLookupComponent>();
                })
                .InitializeInstance();

            var mapManager = sim.Resolve<IMapManager>();

            // Adds the map with id 1, and spawns entity 1 as the map entity.
            mapManager.CreateMap(TestMapId);

            // Adds two grids to use in tests.
            mapManager.CreateGrid(TestMapId, TestGridAId);
            mapManager.CreateGrid(TestMapId, TestGridBId);

            return sim;
        }

        /// <summary>
        ///     Make sure that component state locations are RELATIVE.
        /// </summary>
        [Test]
        public void ComponentStatePositionTest()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var gridA = mapMan.GetGrid(TestGridAId);
            var gridB = mapMan.GetGrid(TestGridBId);

            // Arrange
            var initialPos = new EntityCoordinates(gridA.GridEntityId, (0, 0));
            var parent = entMan.SpawnEntity(null, initialPos);
            var child = entMan.SpawnEntity(null, initialPos);
            var parentTrans = parent.Transform;
            var childTrans = child.Transform;

            var compState = new TransformComponent.TransformComponentState(new Vector2(5, 5), new Angle(0), gridB.GridEntityId, false, false);
            parentTrans.HandleComponentState(compState, null);

            compState = new TransformComponent.TransformComponentState(new Vector2(6, 6), new Angle(0), gridB.GridEntityId, false, false);
            childTrans.HandleComponentState(compState, null);
            // World pos should be 6, 6 now.

            // Act
            var oldWpos = childTrans.WorldPosition;
            compState = new TransformComponent.TransformComponentState(new Vector2(1, 1), new Angle(0), parent.Uid, false, false);
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
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var gridA = mapMan.GetGrid(TestGridAId);
            var gridB = mapMan.GetGrid(TestGridBId);

            // Arrange
            var initalPos = new EntityCoordinates(gridA.GridEntityId, (0, 0));
            var node1 = entMan.SpawnEntity(null, initalPos);
            var node2 = entMan.SpawnEntity(null, initalPos);
            var node3 = entMan.SpawnEntity(null, initalPos);

            node1.Name = "node1_dummy";
            node2.Name = "node2_dummy";
            node3.Name = "node3_dummy";

            var node1Trans = node1.Transform;
            var node2Trans = node2.Transform;
            var node3Trans = node3.Transform;

            var compState = new TransformComponent.TransformComponentState(new Vector2(6, 6), Angle.FromDegrees(135), gridB.GridEntityId, false, false);
            node1Trans.HandleComponentState(compState, null);
            compState = new TransformComponent.TransformComponentState(new Vector2(1, 1), Angle.FromDegrees(45), node1.Uid, false, false);
            node2Trans.HandleComponentState(compState, null);
            compState = new TransformComponent.TransformComponentState(new Vector2(0, 0), Angle.FromDegrees(45), node2.Uid, false, false);
            node3Trans.HandleComponentState(compState, null);

            // Act
            var result = node3Trans.WorldRotation;

            // Assert (135 + 45 + 45 = 225)
            Assert.That(result, new ApproxEqualityConstraint(Angle.FromDegrees(225)));
        }
    }
}
