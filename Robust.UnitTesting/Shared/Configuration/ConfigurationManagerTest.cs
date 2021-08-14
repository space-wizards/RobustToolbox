using NUnit.Framework;
using Robust.Shared.Configuration;

namespace Robust.UnitTesting.Shared.Configuration
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    [TestOf(typeof(ConfigurationManager))]
    public sealed class ConfigurationManagerTest
    {
        [Test]
        public void TestSubscribeUnsubscribe()
        {
            var mgr = new ConfigurationManager();

            mgr.RegisterCVar("foo.bar", 5);

            var lastValue = 0;
            var timesRan = 0;

            void ValueChanged(int value)
            {
                timesRan += 1;
                lastValue = value;
            }

            mgr.OnValueChanged<int>("foo.bar", ValueChanged);

            mgr.SetCVar("foo.bar", 2);

            Assert.That(timesRan, Is.EqualTo(1), "OnValueChanged did not run!");
            Assert.That(lastValue, Is.EqualTo(2), "OnValueChanged value was wrong!");

            mgr.UnsubValueChanged<int>("foo.bar", ValueChanged);

            Assert.That(timesRan, Is.EqualTo(1), "UnsubValueChanged did not unsubscribe!");
        }
    }
}
