using System;
using System.Reflection;
using Moq;
using NUnit.Framework;
using Robust.Shared.Timing;

namespace Robust.UnitTesting.Shared.Timing
{
    [TestFixture]
    [TestOf(typeof(GameLoop))]
    sealed class GameLoop_Test : RobustUnitTest
    {
        /// <summary>
        ///     With single step enabled, the game loop should run 1 tick and then pause again.
        /// </summary>
        [Test]
        [Timeout(1000)] // comment this out if you want to debug
        public void SingleStepTest()
        {
            // TimeoutAttribute causes this to run on different thread on .NET Core,
            // which messes up IoC if we don't run this:
            BaseSetup();
            // Arrange
            var elapsedVal = TimeSpan.FromSeconds(Math.PI);
            var newStopwatch = new Mock<IStopwatch>();
            newStopwatch.SetupGet(p => p.Elapsed).Returns(elapsedVal);
            var gameTiming = GameTimingFactory(newStopwatch.Object);
            gameTiming.Paused = false;
            var loop = new GameLoop(gameTiming);

            var callCount = 0;
            loop.Tick += (sender, args) => callCount++;
            loop.Render += (sender, args) => loop.Running = false; // break the endless loop for testing

            // Act
            loop.SingleStep = true;
            loop.Run();

            // Assert
            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(gameTiming.CurTick, Is.EqualTo(new GameTick(2)));
            Assert.That(gameTiming.Paused, Is.True); // it will pause itself after running each tick
            Assert.That(loop.SingleStep, Is.True); // still true
        }

        private static IGameTiming GameTimingFactory(IStopwatch stopwatch)
        {
            var timing = new GameTiming();

            var field = typeof(GameTiming).GetField("_realTimer", BindingFlags.Instance | BindingFlags.NonPublic)!;
            field.SetValue(timing, stopwatch);

            return timing;
        }
    }
}
