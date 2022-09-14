using Moq;
using NUnit.Framework;
using Robust.Client.GameStates;
using Robust.Client.Timing;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Robust.UnitTesting.Client.GameStates
{
    [TestFixture, Parallelizable, TestOf(typeof(GameStateProcessor))]
    sealed class GameStateProcessor_Tests
    {
        [Test]
        public void FillBufferBlocksProcessing()
        {
            var timingMock = new Mock<IClientGameTiming>();
            timingMock.SetupProperty(p => p.CurTick);

            var timing = timingMock.Object;
            var processor = new GameStateProcessor(timing);
            processor.Interpolation = true;

            processor.AddNewState(GameStateFactory(0, 1));
            processor.AddNewState(GameStateFactory(1, 2)); // buffer is at 2/3, so processing should be blocked

            // calculate states for first tick
            timing.LastProcessedTick = new GameTick(0);
            var result = processor.TryGetServerState(out _, out _);

            Assert.That(result, Is.False);
        }

        [Test]
        public void FillBufferAndCalculateFirstState()
        {
            var timingMock = new Mock<IClientGameTiming>();
            timingMock.SetupProperty(p => p.CurTick);

            var timing = timingMock.Object;
            var processor = new GameStateProcessor(timing);

            processor.AddNewState(GameStateFactory(0, 1));
            processor.AddNewState(GameStateFactory(1, 2));
            processor.AddNewState(GameStateFactory(2, 3)); // buffer is now full, otherwise cannot calculate states.

            // calculate states for first tick
            timing.LastProcessedTick = new GameTick(0);
            var result = processor.TryGetServerState(out var curState, out var nextState);

            Assert.That(result, Is.True);
            Assert.That(curState, Is.Not.Null);
            Assert.That(curState!.ToSequence.Value, Is.EqualTo(1));
            Assert.That(nextState, Is.Null);
        }

        /// <summary>
        ///     When a full state is in the queue (fromSequence = 0), it will modify CurTick to the states' toSequence,
        ///     then return the state as curState.
        /// </summary>
        [Test]
        public void FullStateResyncsCurTick()
        {
            var timingMock = new Mock<IClientGameTiming>();
            timingMock.SetupProperty(p => p.CurTick);

            var timing = timingMock.Object;
            var processor = new GameStateProcessor(timing);

            processor.AddNewState(GameStateFactory(0, 1));
            processor.AddNewState(GameStateFactory(1, 2));
            processor.AddNewState(GameStateFactory(2, 3)); // buffer is now full, otherwise cannot calculate states.

            // calculate states for first tick
            timing.LastProcessedTick = new GameTick(2);
            processor.TryGetServerState(out var state, out _);

            Assert.NotNull(state);
            Assert.That(state!.ToSequence.Value, Is.EqualTo(1));
        }

        [Test]
        public void StatesReceivedPastCurTickAreDropped()
        {
            var (timing, processor) = SetupProcessorFactory();

            // a few moments later...
            timing.LastProcessedTick = new GameTick(4); // current clock is ahead of server
            processor.AddNewState(GameStateFactory(3, 4)); // received a late state
            var result = processor.TryGetServerState(out _, out _);

            Assert.That(result, Is.False);
        }

        /// <summary>
        ///     The server fell behind the client, so the client clock is now ahead of the incoming states.
        ///     Without extrapolation, processing blocks.
        /// </summary>
        [Test]
        public void ServerLagsWithoutExtrapolation()
        {
            var (timing, processor) = SetupProcessorFactory();

            // a few moments later...
            timing.LastProcessedTick = new GameTick(4); // current clock is ahead of server
            var result = processor.TryGetServerState(out _, out _);

            Assert.That(result, Is.False);
        }

        /// <summary>
        ///     There is a hole in the state buffer, we have a future state but their FromSequence is too high!
        ///     In this case we stop and wait for the server to get us the missing link.
        /// </summary>
        [Test]
        public void Hole()
        {
            var (timing, processor) = SetupProcessorFactory();

            processor.AddNewState(GameStateFactory(4, 5));
            timing.LastRealTick = new GameTick(3);
            timing.LastProcessedTick = new GameTick(3);

            var result = processor.TryGetServerState(out _, out _);

            Assert.That(result, Is.False);
        }

        /// <summary>
        ///     Test that the game state manager goes into extrapolation mode *temporarily*,
        ///     if we are missing the curState, but we have a future state that we can apply to skip it.
        /// </summary>
        [Test]
        public void ExtrapolateAdvanceWithFutureState()
        {
            var (timing, processor) = SetupProcessorFactory();

            processor.Interpolation = true;

            timing.LastProcessedTick = new GameTick(3);

            timing.LastRealTick = new GameTick(3);
            processor.AddNewState(GameStateFactory(3, 5));

            // We're missing the state for this tick so go into extrap.
            var result = processor.TryGetServerState(out var curState, out _);

            Assert.That(result, Is.True);
            Assert.That(curState, Is.Null);

            timing.LastProcessedTick = new GameTick(4);

            // But we DO have the state for the tick after so apply away!
            result = processor.TryGetServerState(out curState, out _);

            Assert.That(result, Is.True);
            Assert.That(curState, Is.Not.Null);
        }

        /// <summary>
        ///     Creates a new empty GameState with the given to and from properties.
        /// </summary>
        private static GameState GameStateFactory(uint from, uint to)
        {
            return new(new GameTick(@from), new GameTick(to), 0, default, default, default, null);
        }

        /// <summary>
        ///     Creates a new GameTiming and GameStateProcessor, fills the processor with enough states, and calculate the first tick.
        ///     CurTick = 1, states 1 - 3 are in the buffer.
        /// </summary>
        private static (IClientGameTiming timing, GameStateProcessor processor) SetupProcessorFactory()
        {
            var timingMock = new Mock<IClientGameTiming>();
            timingMock.SetupProperty(p => p.CurTick);
            timingMock.SetupProperty(p => p.LastProcessedTick);
            timingMock.SetupProperty(p => p.LastRealTick);
            timingMock.SetupProperty(p => p.TickTimingAdjustment);

            var timing = timingMock.Object;
            var processor = new GameStateProcessor(timing);

            processor.AddNewState(GameStateFactory(0, 1));
            processor.AddNewState(GameStateFactory(1, 2));
            processor.AddNewState(GameStateFactory(2, 3)); // buffer is now full, otherwise cannot calculate states.

            processor.LastFullStateRequested = null;
            timing.LastProcessedTick = timing.LastRealTick = new GameTick(1);

            return (timing, processor);
        }
    }
}
