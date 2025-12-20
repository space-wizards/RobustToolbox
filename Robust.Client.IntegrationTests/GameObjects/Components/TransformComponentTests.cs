using System.Numerics;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Client.GameObjects.Components
{
    [TestFixture]
    [TestOf(typeof(TransformComponent))]
    public sealed class TransformComponentTests
    {
        private static (ISimulation, EntityUid gridA, EntityUid gridB)  SimulationFactory()
        {
            var sim = RobustServerSimulation
                .NewSimulation()
                .InitializeInstance();

            var mapId = sim.Resolve<IEntityManager>().System<SharedMapSystem>().CreateMap();
            var mapManager = sim.Resolve<IMapManager>();

            // Adds two grids to use in tests.
            var gridA = mapManager.CreateGridEntity(mapId);
            var gridB = mapManager.CreateGridEntity(mapId);

            return (sim, gridA, gridB);
        }

        /// <summary>
        ///     Make sure that component state locations are RELATIVE.
        /// </summary>
        [Test]
        public void ComponentStatePositionTest()
        {
            var (sim, gridIdA, gridIdB) = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var xformSystem = entMan.System<SharedTransformSystem>();
            var gridSystem = entMan.System<SharedGridTraversalSystem>();
            gridSystem.Enabled = false;

            // Arrange
            var initialPos = new EntityCoordinates(gridIdA, new Vector2(0, 0));
            var parent = entMan.SpawnEntity(null, initialPos);
            var child = entMan.SpawnEntity(null, initialPos);
            var parentTrans = entMan.GetComponent<TransformComponent>(parent);
            var childTrans = entMan.GetComponent<TransformComponent>(child);
            ComponentHandleState handleState;

            var compState = new TransformComponentState(new Vector2(5, 5), new Angle(0), entMan.GetNetEntity(gridIdB), false, false);
            handleState = new ComponentHandleState(compState, null);
            xformSystem.OnHandleState(parent, parentTrans, ref handleState);

            compState = new TransformComponentState(new Vector2(6, 6), new Angle(0), entMan.GetNetEntity(gridIdB), false, false);
            handleState = new ComponentHandleState(compState, null);
            xformSystem.OnHandleState(child, childTrans, ref handleState);
            // World pos should be 6, 6 now.

            // Act
            var oldWpos = xformSystem.GetWorldPosition(childTrans);
            compState = new TransformComponentState(new Vector2(1, 1), new Angle(0), entMan.GetNetEntity(parent), false, false);
            handleState = new ComponentHandleState(compState, null);
            xformSystem.OnHandleState(child, childTrans, ref handleState);
            var newWpos = xformSystem.GetWorldPosition(childTrans);

            gridSystem.Enabled = true;

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
            var xformSystem = entMan.System<SharedTransformSystem>();
            var metaSystem = entMan.System<MetaDataSystem>();
            var gridSystem = entMan.System<SharedGridTraversalSystem>();
            gridSystem.Enabled = false;

            // Arrange
            var initalPos = new EntityCoordinates(gridIdA, new Vector2(0, 0));
            var node1 = entMan.SpawnEntity(null, initalPos);
            var node2 = entMan.SpawnEntity(null, initalPos);
            var node3 = entMan.SpawnEntity(null, initalPos);

            metaSystem.SetEntityName(node1, "node1_dummy");
            metaSystem.SetEntityName(node2, "node2_dummy");
            metaSystem.SetEntityName(node3, "node3_dummy");

            var node1Trans = entMan.GetComponent<TransformComponent>(node1);
            var node2Trans = entMan.GetComponent<TransformComponent>(node2);
            var node3Trans = entMan.GetComponent<TransformComponent>(node3);

            var compState = new TransformComponentState(new Vector2(6, 6), Angle.FromDegrees(135), entMan.GetNetEntity(gridIdB), false, false);
            var handleState = new ComponentHandleState(compState, null);
            xformSystem.OnHandleState(node1, node1Trans, ref handleState);

            compState = new TransformComponentState(new Vector2(1, 1), Angle.FromDegrees(45), entMan.GetNetEntity(node1), false, false);
            handleState = new ComponentHandleState(compState, null);
            xformSystem.OnHandleState(node2, node2Trans, ref handleState);

            compState = new TransformComponentState(new Vector2(0, 0), Angle.FromDegrees(45), entMan.GetNetEntity(node2), false, false);
            handleState = new ComponentHandleState(compState, null);
            xformSystem.OnHandleState(node3, node3Trans, ref handleState);

            // Act
            var result = xformSystem.GetWorldRotation(node3Trans);

            gridSystem.Enabled = true;

            // Assert (135 + 45 + 45 = 225)
            Assert.That(result, new ApproxEqualityConstraint(Angle.FromDegrees(225)));
        }
    }
}
