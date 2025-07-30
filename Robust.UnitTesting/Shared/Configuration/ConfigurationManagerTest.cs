using Moq;
using NUnit.Framework;
using Robust.Server.Configuration;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Replays;
using Robust.Shared.Timing;

namespace Robust.UnitTesting.Shared.Configuration
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    [TestOf(typeof(ConfigurationManager))]
    public sealed class ConfigurationManagerTest
    {
        private IConfigurationManager _mgr;

        [SetUp]
        public void Setup()
        {
            _mgr = MakeCfg();
        }

        [Test]
        public void TestSubscribeUnsubscribe()
        {

            _mgr.RegisterCVar("foo.bar", 5);

            var lastValue = 0;
            var timesRan = 0;

            void ValueChanged(int value)
            {
                timesRan += 1;
                lastValue = value;
            }

            _mgr.OnValueChanged<int>("foo.bar", ValueChanged);

            _mgr.SetCVar("foo.bar", 2);

            Assert.That(timesRan, Is.EqualTo(1), "OnValueChanged did not run!");
            Assert.That(lastValue, Is.EqualTo(2), "OnValueChanged value was wrong!");

            _mgr.UnsubValueChanged<int>("foo.bar", ValueChanged);

            Assert.That(timesRan, Is.EqualTo(1), "UnsubValueChanged did not unsubscribe!");
        }

        [Test]
        public void TestSubscribeUnsubscribe1()
        {
            _mgr.RegisterCVar("foo.bar", 5);
            _mgr.RegisterCVar("foo.foo", 2);

            var lastValueBar = 0;
            var lastValueFoo = 0;

            var subscription = _mgr.SubscribeMultiple()
                                   .OnValueChanged<int>("foo.bar", value => lastValueBar = value)
                                   .OnValueChanged<int>("foo.foo", value => lastValueFoo = value)
                                   .Subscribe();

            _mgr.SetCVar("foo.bar", 1);
            _mgr.SetCVar("foo.foo", 3);

            Assert.That(lastValueBar, Is.EqualTo(1), "OnValueChanged value was wrong!");
            Assert.That(lastValueFoo, Is.EqualTo(3), "OnValueChanged value was wrong!");

            subscription.Dispose();

            _mgr.SetCVar("foo.bar", 10);
            _mgr.SetCVar("foo.foo", 30);

            Assert.That(lastValueBar, Is.EqualTo(1), "OnValueChanged value was wrong!");
            Assert.That(lastValueFoo, Is.EqualTo(3), "OnValueChanged value was wrong!");
        }

        [Test]
        public void TestOverrideDefaultValue()
        {
            _mgr.RegisterCVar("foo.bar", 5);

            var value = 0;
            _mgr.OnValueChanged<int>("foo.bar", v => value = v);

            // Change default value, this fires the value changed callback.
            _mgr.OverrideDefault("foo.bar", 10);

            Assert.That(value, Is.EqualTo(10));
            Assert.That(_mgr.GetCVar<int>("foo.bar"), Is.EqualTo(10));

            // Modify the cvar programmatically, also fires the callback.
            _mgr.SetCVar("foo.bar", 7);

            Assert.That(value, Is.EqualTo(7));
            Assert.That(_mgr.GetCVar<int>("foo.bar"), Is.EqualTo(7));

            // We have a value set now, so changing the default won't do anything.
            _mgr.OverrideDefault("foo.bar", 15);

            Assert.That(value, Is.EqualTo(7));
            Assert.That(_mgr.GetCVar<int>("foo.bar"), Is.EqualTo(7));
        }

        private IConfigurationManager MakeCfg()
        {
            var collection = new DependencyCollection();
            collection.RegisterInstance<IReplayRecordingManager>(new Mock<IReplayRecordingManager>().Object);
            collection.RegisterInstance<INetManager>(new Mock<INetManager>().Object);
            collection.Register<ConfigurationManager, ServerNetConfigurationManager>();
            collection.Register<IServerNetConfigurationManager, ServerNetConfigurationManager>();
            collection.Register<IGameTiming, GameTiming>();
            collection.Register<ILogManager, LogManager>();
            collection.BuildGraph();

            return collection.Resolve<ConfigurationManager>();
        }
    }
}
