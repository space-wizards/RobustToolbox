using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Robust.Shared.Asynchronous;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Timers;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Timer = Robust.Shared.Timers.Timer;

namespace Robust.UnitTesting.Shared.Timers
{
    [TestFixture]
    [TestOf(typeof(Timer))]
    public class TimerTest : RobustUnitTest
    {
        private LogCatcher _catcher;

        [OneTimeSetUp]
        public void InstallLogCatcher()
        {
            var logManager = IoCManager.Resolve<ILogManager>();

            _catcher = new LogCatcher();
            logManager.GetSawmill("timer").AddHandler(_catcher);
            logManager.GetSawmill("async").AddHandler(_catcher);
        }

        [Test]
        public void TestSpawn()
        {
            var timerManager = IoCManager.Resolve<ITimerManager>();

            var fired = false;

            Timer.Spawn(TimeSpan.FromMilliseconds(500), () => fired = true);
            Assert.That(fired, Is.False);

            // Set timers ahead 250 ms
            timerManager.UpdateTimers(0.25f);
            Assert.That(fired, Is.False);
            // Another 300ms should do it.
            timerManager.UpdateTimers(0.30f);
            Assert.That(fired, Is.True);
        }

        [Test]
        public void TestSpawnedException()
        {
            var timerManager = IoCManager.Resolve<ITimerManager>();
            _catcher.Flush();

            bool DidThrow()
            {
                // TODO: Probably not perfect to hard code the reliance on logging.
                // Logging should be kinda user-facing purely.
                // Some sorta structured logging for exceptions like we have in SS13 would be good.
                return _catcher.CaughtLogs.Any(l => l.Level == LogLevel.Error);
            }

            var threw = false;

            Timer.Spawn(TimeSpan.FromMilliseconds(500), () =>
            {
                threw = true;
                throw new InvalidOperationException();
            });

            Assert.That(threw, Is.False);
            Assert.That(DidThrow(), Is.False);

            // Set timers ahead 250 ms
            timerManager.UpdateTimers(0.25f);
            Assert.That(threw, Is.False);
            Assert.That(DidThrow(), Is.False);

            // Another 300ms should do it.
            timerManager.UpdateTimers(0.30f);
            Assert.That(threw, Is.True);
            Assert.That(DidThrow(), Is.True);
        }

        [Test]
        public void TestAsyncDelay()
        {
            var timerManager = IoCManager.Resolve<ITimerManager>();
            var ran = false;

            async void Run()
            {
                await Timer.Delay(TimeSpan.FromMilliseconds(500));
                ran = true;
            }

            Run();
            Assert.That(ran, Is.False);

            // Set timers ahead 250 ms
            timerManager.UpdateTimers(0.25f);
            Assert.That(ran, Is.False);

            // Another 300ms should do it.
            timerManager.UpdateTimers(0.30f);
            Assert.That(ran, Is.True);
        }

        [Test]
        public void TestAsyncDelayException()
        {
            var timerManager = IoCManager.Resolve<ITimerManager>();
            var taskManager = IoCManager.Resolve<ITaskManager>();

            taskManager.Initialize();
            _catcher.Flush();

            bool DidThrow()
            {
                // TODO: Probably not perfect to hard code the reliance on logging.
                // Logging should be kinda user-facing purely.
                // Some sorta structured logging for exceptions like we have in SS13 would be good.
                return _catcher.CaughtLogs.Any(l => l.Level == LogLevel.Error);
            }

            var threw = false;

            async void Run()
            {
                await Timer.Delay(TimeSpan.FromMilliseconds(500));
                threw = true;
                throw new InvalidOperationException();
            }

            Run();
            Assert.That(threw, Is.False);
            Assert.That(DidThrow(), Is.False);

            // Set timers ahead 250 ms
            timerManager.UpdateTimers(0.25f);
            Assert.That(threw, Is.False);
            Assert.That(DidThrow(), Is.False);

            // Another 300ms should do it.
            timerManager.UpdateTimers(0.30f);
            taskManager.ProcessPendingTasks();
            Assert.That(threw, Is.True);
            Assert.That(DidThrow(), Is.True);
        }

        [Test]
        public void TestCancellation()
        {
            var timerManager = IoCManager.Resolve<ITimerManager>();
            var taskManager = IoCManager.Resolve<ITaskManager>();

            var cts = new CancellationTokenSource();
            var ran = false;
            Timer.Spawn(1000, () => ran = true, cts.Token);

            timerManager.UpdateTimers(0.5f);

            Assert.That(ran, Is.False);

            cts.Cancel();

            timerManager.UpdateTimers(0.6f);

            Assert.That(ran, Is.False);
        }
    }
}
