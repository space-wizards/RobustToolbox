using System;
using System.Reflection;
using Moq;
using NUnit.Framework;
using Robust.Shared.Timing;

namespace Robust.UnitTesting.Shared.Timing
{
    [TestFixture]
    [TestOf(typeof(GameTiming))]
    class GameTiming_Test
    {
        /// <summary>
        ///     Checks that IGameTiming.RealTime returns the real(wall) uptime since the stopwatch was started.
        /// </summary>
        /// <remarks>
        ///     This is unaffected by pausing, and has nothing to do with the simulation. This should be used for
        ///     out-of-simulation timing, for example sound timing, UI timing, input timing, etc.
        /// </remarks>
        [Test]
        public void RealTimeTest()
        {
            // Arrange
            var elapsedVal = TimeSpan.FromSeconds(Math.PI);
            var newStopwatch = new Mock<IStopwatch>();
            newStopwatch.SetupGet(p => p.Elapsed).Returns(elapsedVal);
            var gameTiming = GameTimingFactory(newStopwatch.Object);

            // Act
            gameTiming.StartFrame();
            var result = gameTiming.RealTime;

            // Assert
            Assert.That(result, Is.EqualTo(elapsedVal));
        }

        /// <summary>
        ///     Checks that IGameTiming.RealFrameTime returns the real(wall) delta time between the two most recent calls to
        ///     IGameTiming.StartFrame().
        /// </summary>
        /// <remarks>
        ///     This is unaffected by pausing, and has nothing to do with the simulation. This value is used
        ///     by the profiling functions.
        /// </remarks>
        [Test]
        public void RealFrameTimeTest()
        {
            // Arrange
            var elapsedVal = TimeSpan.FromSeconds(3);
            var newStopwatch = new Mock<IStopwatch>();
            newStopwatch.SetupGet(p => p.Elapsed).Returns(() => elapsedVal);
            var gameTiming = GameTimingFactory(newStopwatch.Object);
            gameTiming.StartFrame(); // changes last time from 0 to 3

            // Act
            elapsedVal = TimeSpan.FromSeconds(5);
            gameTiming.StartFrame();
            var result = gameTiming.RealFrameTime;

            // Assert
            Assert.That(result, Is.EqualTo(TimeSpan.FromSeconds(2))); // 5 - 3 = 2
        }

        /// <summary>
        ///     Checks that IGameTiming.CurTime returns the current simulation uptime when inside the simulation.
        /// </summary>
        /// <remarks>
        ///     This value is affected by pausing. This value is derived from CurTick and TickRate, and is unaffected
        ///     by RealTime. All simulation code should be using this value to measure uptime.
        /// </remarks>
        [Test]
        public void InSimCurTimeTest()
        {
            // Arrange
            var newStopwatch = new Mock<IStopwatch>();
            var gameTiming = GameTimingFactory(newStopwatch.Object);
            gameTiming.InSimulation = true;

            //NOTE: TickRate can cause a slight rounding error in TickPeriod reciprocal calculation from repeating decimals depending
            // on the value chosen.
            gameTiming.TickRate = 20;
            gameTiming.CurTick = new GameTick(61); // 1 + 60, because 1 is the first tick

            // Act
            gameTiming.StartFrame();
            var result = gameTiming.CurTime;

            // Assert
            var expected = TimeSpan.FromTicks(TimeSpan.TicksPerSecond * 3);
            Assert.That(result, Is.EqualTo(expected));
        }

        /// <summary>
        ///     Checks that IGameTiming.CurTime returns the current simulation time + fractional tick time when outside the
        ///     simulation.
        /// </summary>
        /// <remarks>
        ///     This is the same thing as in-simulation CurTime, but also adds the fractional tick to the time. This is useful
        ///     for the renderer to be able to transparently cause the simulation to extrapolate between CurTick and CurTick + 1.
        /// </remarks>
        [Test]
        public void OutSimCurTimeTest()
        {
            // Arrange
            var newStopwatch = new Mock<IStopwatch>();
            var gameTiming = GameTimingFactory(newStopwatch.Object);
            gameTiming.InSimulation = false;
            gameTiming.TickRate = 20;
            gameTiming.CurTick = new GameTick(61); // 1 + 60, because 1 is the first tick
            gameTiming.TickRemainder = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 2); // half a second

            // Act
            gameTiming.StartFrame();
            var result = gameTiming.CurTime;

            // Assert
            var expected = TimeSpan.FromSeconds(3.5);
            Assert.That(result, Is.EqualTo(expected));
        }

        /// <summary>
        ///     Checks that IGameTiming.FrameTime returns the simulated delta time between the two most recent calls to IGameTiming.StartFrame().
        /// </summary>
        /// <remarks>
        ///     This value is not affected by pausing. This value is derived from TickRate, and is unaffected
        ///     by RealTime. There is no lag or jitter inside the simulation. The FrameTime is always exactly TickPeriod.
        /// </remarks>
        [Test]
        public void InSimFrameTimeTest()
        {
            // Arrange
            var newStopwatch = new Mock<IStopwatch>();
            var gameTiming = GameTimingFactory(newStopwatch.Object);
            gameTiming.InSimulation = true;
            gameTiming.TickRate = 20;

            // Act
            gameTiming.StartFrame();
            gameTiming.CurTick = new GameTick(gameTiming.CurTick.Value + 1);
            gameTiming.StartFrame();
            var result = gameTiming.FrameTime;

            // Assert
            var expected = TimeSpan.FromTicks((long)(TimeSpan.TicksPerSecond * (1/20.0)));
            Assert.That(result, Is.EqualTo(expected));
        }

        /// <summary>
        ///     Checks that IGameTiming.FrameTime returns the real delta time between the two most recent calls to IGameTiming.StartFrame().
        /// </summary>
        /// <remarks>
        ///     When outside the simulation, FrameTime returns the same value as RealFrameTime. This allows rendering code to also use
        ///     the FrameTime property instead of RealFrameTime.
        /// </remarks>
        [Test]
        public void OutSimFrameTimeTest()
        {
            // Arrange
            var elapsedVal = TimeSpan.FromSeconds(3);
            var newStopwatch = new Mock<IStopwatch>();
            newStopwatch.SetupGet(p => p.Elapsed).Returns(() => elapsedVal);
            var gameTiming = GameTimingFactory(newStopwatch.Object);
            gameTiming.InSimulation = false;
            gameTiming.Paused = false; // paused timing returns 0 frame time.
            gameTiming.StartFrame(); // changes last time from 0 to 3

            // Act
            elapsedVal = TimeSpan.FromSeconds(5);
            gameTiming.StartFrame();
            var result = gameTiming.FrameTime;

            // Assert
            Assert.That(result, Is.EqualTo(TimeSpan.FromSeconds(2))); // 5 - 3 = 2
        }

        /// <summary>
        ///     Checks that IGameTiming.FrameTime returns zero when outside the simulation, and the simulation is paused.
        /// </summary>
        /// <remarks>
        ///     If the simulation is paused, then the update loop should not be ran. When rendering calls code that uses FrameTime
        ///     while paused, time never passed since the last frame, and FrameTime should always return 0.
        /// </remarks>
        [Test]
        public void OutSimFrameTimePausedTest()
        {
            // Arrange
            var elapsedVal = TimeSpan.FromSeconds(3);
            var newStopwatch = new Mock<IStopwatch>();
            newStopwatch.SetupGet(p => p.Elapsed).Returns(() => elapsedVal);
            var gameTiming = GameTimingFactory(newStopwatch.Object);
            gameTiming.InSimulation = false;
            gameTiming.StartFrame(); // changes last time from 0 to 3
            gameTiming.Paused = true;

            // Act
            elapsedVal = TimeSpan.FromSeconds(5); // RealTime increases
            gameTiming.StartFrame();
            var result = gameTiming.FrameTime;

            // Assert
            Assert.That(result, Is.EqualTo(TimeSpan.Zero)); // But simulation time never increases.
        }

        private static IGameTiming GameTimingFactory(IStopwatch stopwatch)
        {
            var timing = new GameTiming();

            var field = typeof(GameTiming).GetField("_realTimer", BindingFlags.NonPublic | BindingFlags.Instance)!;
            field.SetValue(timing, stopwatch);

            Assert.That(timing.CurTime, Is.EqualTo(TimeSpan.Zero));

            return timing;
        }
    }
}
