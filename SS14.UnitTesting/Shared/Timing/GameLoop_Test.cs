using System;
using System.Reflection;
using Moq;
using NUnit.Framework;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.Timing;

namespace SS14.UnitTesting.Shared.Timing
{
    [TestFixture]
    [TestOf(typeof(GameLoop))]
    class GameLoop_Test
    {
        /// <summary>
        ///     With single step enabled, the game loop should run 1 tick and then pause again.
        /// </summary>
        [Test]
        [Timeout(1000)] // comment this out if you want to debug
        public void SingleStepTest()
        {
            // Arrange
            var elapsedVal = TimeSpan.FromSeconds(Math.PI);
            var newStopwatch = new Mock<IStopwatch>();
            newStopwatch.SetupGet(p => p.Elapsed).Returns(elapsedVal);
            var gameTiming = GameTimingFactory(newStopwatch.Object);
            var loop = new GameLoop(gameTiming);

            var callCount = 0;
            loop.Tick += (sender, args) => callCount++;
            loop.Render += (sender, args) => loop.Running = false; // break the endless loop for testing

            // Act
            loop.SingleStep = true;
            loop.Run();

            // Assert
            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(gameTiming.CurTick, Is.EqualTo(1));
            Assert.That(gameTiming.Paused, Is.True); // it will pause itself after running each tick
            Assert.That(loop.SingleStep, Is.True); // still true
        }

        private static IGameTiming GameTimingFactory(IStopwatch stopwatch)
        {
            var timing = new GameTiming();

            var field = typeof(GameTiming).GetField("_realTimer", BindingFlags.Static | BindingFlags.NonPublic);
            field.SetValue(null, stopwatch);

            return timing;
        }
    }
}
