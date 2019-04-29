using Moq;
using NUnit.Framework;
using Robust.Client.GameStates;
using Robust.Shared.GameStates;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.Timing;

namespace Robust.UnitTesting.Client.GameStates
{
    [TestFixture, Parallelizable, TestOf(typeof(GameStateProcessor))]
    class GameStateProcessor_Tests
    {
        [Test]
        public void FillBufferAndCalculateFirstState()
        {
            uint curTick = 1;
            var gameTiming = new Mock<IGameTiming>();
            gameTiming.SetupGet(f => f.CurTick).Returns((() => new GameTick(curTick)));

            var stateMan = new GameStateProcessor(gameTiming.Object);

            stateMan.AddNewState(GameStateFactory(0, 1), 0);
            stateMan.AddNewState(GameStateFactory(1, 2), 0);
            stateMan.AddNewState(GameStateFactory(2, 3), 0); // buffer is now full

            // calculate states for first tick
            var result = stateMan.TryCalculateStates(new GameTick(curTick), out var curState, out var nextState);

            Assert.That(result, Is.True);
            Assert.That(curState, Is.Not.Null);
            Assert.That(curState.ToSequence.Value, Is.EqualTo(1));
            Assert.That(nextState, Is.Null);
        }

        private GameState GameStateFactory(uint from, uint to)
        {
            return new GameState(new GameTick(from), new GameTick(to), null, null, null, null);
        }
    }
}
