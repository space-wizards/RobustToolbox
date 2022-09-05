using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Client.GameObjects.Components
{
    [TestFixture]
    [TestOf(typeof(TransformComponent))]
    public sealed class TransformComponentTests
    {
        private static readonly MapId TestMapId = new(1);

        private static (ISimulation, EntityUid gridA, EntityUid gridB)  SimulationFactory()
        {
            var sim = RobustServerSimulation
                .NewSimulation()
                .InitializeInstance();

            var mapManager = sim.Resolve<IMapManager>();

            // Adds the map with id 1, and spawns entity 1 as the map entity.
            mapManager.CreateMap(TestMapId);

            // Adds two grids to use in tests.
            var gridA = mapManager.CreateGrid(TestMapId);
            var gridB = mapManager.CreateGrid(TestMapId);

            return (sim, gridA.GridEntityId, gridB.GridEntityId);
        }

        /// <summary>
        ///     Make sure that component state locations are RELATIVE.
        /// </summary>
        [Test]
        public void ComponentStatePositionTest()
        {
            var (sim, gridIdA, gridIdB) = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var gridA = mapMan.GetGrid(gridIdA);
            var gridB = mapMan.GetGrid(gridIdB);

            // Arrange
            var initialPos = new EntityCoordinates(gridA.GridEntityId, (0, 0));
            var parent = entMan.SpawnEntity(null, initialPos);
            var child = entMan.SpawnEntity(null, initialPos);
            var parentTrans = entMan.GetComponent<TransformComponent>(parent);
            var childTrans = entMan.GetComponent<TransformComponent>(child);

            var compState = new TransformComponentState(new Vector2(5, 5), new Angle(0), gridB.GridEntityId, false, false);
            parentTrans.HandleComponentState(compState, null);

            compState = new TransformComponentState(new Vector2(6, 6), new Angle(0), gridB.GridEntityId, false, false);
            childTrans.HandleComponentState(compState, null);
            // World pos should be 6, 6 now.

            // Act
            var oldWpos = childTrans.WorldPosition;
            compState = new TransformComponentState(new Vector2(1, 1), new Angle(0), parent, false, false);
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
            var (sim, gridIdA, gridIdB) = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();
            var xformSystem = sim.Resolve<IEntitySystemManager>().GetEntitySystem<SharedTransformSystem>();

            var gridA = mapMan.GetGrid(gridIdA);
            var gridB = mapMan.GetGrid(gridIdB);

            // Arrange
            var initalPos = new EntityCoordinates(gridA.GridEntityId, (0, 0));
            var node1 = entMan.SpawnEntity(null, initalPos);
            var node2 = entMan.SpawnEntity(null, initalPos);
            var node3 = entMan.SpawnEntity(null, initalPos);

            entMan.GetComponent<MetaDataComponent>(node1).EntityName = "node1_dummy";
            entMan.GetComponent<MetaDataComponent>(node2).EntityName = "node2_dummy";
            entMan.GetComponent<MetaDataComponent>(node3).EntityName = "node3_dummy";

            var node1Trans = entMan.GetComponent<TransformComponent>(node1);
            var node2Trans = entMan.GetComponent<TransformComponent>(node2);
            var node3Trans = entMan.GetComponent<TransformComponent>(node3);

            var compState = new TransformComponentState(new Vector2(6, 6), Angle.FromDegrees(135), gridB.GridEntityId, false, false);
            var handleState = new ComponentHandleState(compState, null);
            xformSystem.OnHandleState(node1, node1Trans, ref handleState);

            compState = new TransformComponentState(new Vector2(1, 1), Angle.FromDegrees(45), node1, false, false);
            handleState = new ComponentHandleState(compState, null);
            xformSystem.OnHandleState(node2, node2Trans, ref handleState);

            compState = new TransformComponentState(new Vector2(0, 0), Angle.FromDegrees(45), node2, false, false);
            handleState = new ComponentHandleState(compState, null);
            xformSystem.OnHandleState(node3, node3Trans, ref handleState);

            // Act
            var result = node3Trans.WorldRotation;

            // Assert (135 + 45 + 45 = 225)
            Assert.That(result, new ApproxEqualityConstraint(Angle.FromDegrees(225)));
        }
    }
}
