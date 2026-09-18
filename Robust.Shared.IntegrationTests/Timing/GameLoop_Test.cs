using System;
using System.Reflection;
using Moq;
using NUnit.Framework;
using Robust.Shared.Exceptions;
using Robust.Shared.Log;
using Robust.Shared.Profiling;
using Robust.Shared.Timing;

namespace Robust.UnitTesting.Shared.Timing
{
    [TestFixture]
    [TestOf(typeof(GameLoop))]
    sealed class GameLoop_Test : OurRobustUnitTest
    {
        /// <summary>
        ///     With single step enabled, the game loop should run 1 tick and then pause again.
        /// </summary>
        [Test]
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
            var loop = new GameLoop(
                gameTiming,
                new RuntimeLog(),
                new ProfManager(),
                new LogManager().RootSawmill,
                new GameLoopOptions(false));

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


        [Test]
        public void TimingAdjustmentChangeMidTickDoesNotShortenActiveTick()
        {
            BaseSetup();

            var elapsedVal = TimeSpan.FromSeconds(0.05);
            var newStopwatch = new Mock<IStopwatch>();
            newStopwatch.SetupGet(p => p.Elapsed).Returns(() => elapsedVal);
            var gameTiming = GameTimingFactory(newStopwatch.Object);
            gameTiming.Paused = false;
            gameTiming.TickRate = 10;
            gameTiming.TickTimingAdjustment = 0f;

            var loop = new GameLoop(
                gameTiming,
                new RuntimeLog(),
                new ProfManager(),
                new LogManager().RootSawmill,
                new GameLoopOptions(false));

            var ticks = 0;
            var renders = 0;
            loop.Tick += (_, _) => ticks++;
            loop.Render += (_, _) =>
            {
                renders++;
                if (renders == 1)
                {
                    Assert.That(gameTiming.TickPhase, Is.EqualTo(0.5f).Within(0.0001f));
                    gameTiming.TickTimingAdjustment = 0.1f;
                    Assert.That(gameTiming.TickPhase, Is.EqualTo(0.5f).Within(0.0001f));
                    elapsedVal = TimeSpan.FromSeconds(0.18);
                    return;
                }

                loop.Running = false;
            };

            loop.Run();

            Assert.Multiple(() =>
            {
                Assert.That(ticks, Is.EqualTo(1));
                Assert.That(gameTiming.CurTick, Is.EqualTo(new GameTick(2)));
                Assert.That(gameTiming.LastTick, Is.EqualTo(TimeSpan.FromSeconds(0.1)).Within(TimeSpan.FromTicks(1)));
                Assert.That(gameTiming.TickRemainderRealtime, Is.EqualTo(TimeSpan.FromSeconds(0.08)).Within(TimeSpan.FromTicks(1)));
                Assert.That(gameTiming.TickPhase, Is.EqualTo(0.08f / 0.09f).Within(0.0001f));
            });
        }

        private static GameTiming GameTimingFactory(IStopwatch stopwatch)
        {
            var timing = new GameTiming();

            var field = typeof(GameTiming).GetField("_realTimer", BindingFlags.Instance | BindingFlags.NonPublic)!;
            field.SetValue(timing, stopwatch);

            return timing;
        }
    }
}
