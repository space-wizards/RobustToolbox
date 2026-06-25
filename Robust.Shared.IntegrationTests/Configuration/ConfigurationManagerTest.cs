using Moq;
using NUnit.Framework;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Replays;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.IntegrationTests.Configuration
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    [TestOf(typeof(ConfigurationManager))]
    internal sealed class ConfigurationManagerTest
    {
        [Test]
        public void TestSubscribeUnsubscribe()
        {
            var mgr = MakeCfg();

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

        [Test]
        public void TestSubscribe_SubscribeMultipleThenUnsubscribe()
        {
            var mgr = MakeCfg();

            mgr.RegisterCVar("foo.bar", 5);

            var lastValueBar1 = 0;
            var lastValueBar2 = 0;
            var lastValueBar3 = 0;
            var lastValueBar4 = 0;

            var subscription = mgr.SubscribeMultiple()
                .OnValueChanged<int>("foo.bar", value => lastValueBar1 = value)
                .OnValueChanged<int>("foo.bar", value => lastValueBar2 = value)
                .OnValueChanged<int>("foo.bar", value => lastValueBar3 = value)
                .OnValueChanged<int>("foo.bar", value => lastValueBar4 = value);

            mgr.SetCVar("foo.bar", 1);

            Assert.That(lastValueBar1, Is.EqualTo(1), "OnValueChanged value was wrong!");
            Assert.That(lastValueBar2, Is.EqualTo(1), "OnValueChanged value was wrong!");
            Assert.That(lastValueBar3, Is.EqualTo(1), "OnValueChanged value was wrong!");
            Assert.That(lastValueBar4, Is.EqualTo(1), "OnValueChanged value was wrong!");

            subscription.Dispose();

            mgr.SetCVar("foo.bar", 10);

            Assert.That(lastValueBar1, Is.EqualTo(1), "OnValueChanged value was wrong!");
            Assert.That(lastValueBar2, Is.EqualTo(1), "OnValueChanged value was wrong!");
            Assert.That(lastValueBar3, Is.EqualTo(1), "OnValueChanged value was wrong!");
            Assert.That(lastValueBar4, Is.EqualTo(1), "OnValueChanged value was wrong!");
        }

        [Test]
        public void TestSubscribe_Unsubscribe()
        {
            var mgr = MakeCfg();

            mgr.RegisterCVar("foo.bar", 5);
            mgr.RegisterCVar("foo.foo", 2);

            var lastValueBar = 0;
            var lastValueFoo = 0;

            var subscription = mgr.SubscribeMultiple()
                .OnValueChanged<int>("foo.bar", value => lastValueBar = value)
                .OnValueChanged<int>("foo.foo", value => lastValueFoo = value);

            mgr.SetCVar("foo.bar", 1);
            mgr.SetCVar("foo.foo", 3);

            Assert.That(lastValueBar, Is.EqualTo(1), "OnValueChanged value was wrong!");
            Assert.That(lastValueFoo, Is.EqualTo(3), "OnValueChanged value was wrong!");

            subscription.Dispose();

            mgr.SetCVar("foo.bar", 10);
            mgr.SetCVar("foo.foo", 30);

            Assert.That(lastValueBar, Is.EqualTo(1), "OnValueChanged value was wrong!");
            Assert.That(lastValueFoo, Is.EqualTo(3), "OnValueChanged value was wrong!");
        }

        [Test]
        public void TestOverrideDefaultValue()
        {
            var mgr = MakeCfg();

            mgr.RegisterCVar("foo.bar", 5);

            var value = 0;
            mgr.OnValueChanged<int>("foo.bar", v => value = v);

            // Change default value, this fires the value changed callback.
            mgr.OverrideDefault("foo.bar", 10);

            Assert.That(value, Is.EqualTo(10));
            Assert.That(mgr.GetCVar<int>("foo.bar"), Is.EqualTo(10));

            // Modify the cvar programmatically, also fires the callback.
            mgr.SetCVar("foo.bar", 7);

            Assert.That(value, Is.EqualTo(7));
            Assert.That(mgr.GetCVar<int>("foo.bar"), Is.EqualTo(7));

            // We have a value set now, so changing the default won't do anything.
            mgr.OverrideDefault("foo.bar", 15);

            Assert.That(value, Is.EqualTo(7));
            Assert.That(mgr.GetCVar<int>("foo.bar"), Is.EqualTo(7));
        }

        [Test]
        public void TestClientSaveDoesNotSerializeServerCVars()
        {
            var mgr = MakeCfgInternal(isServer: false);
            var userData = new VirtualWritableDirProvider();
            var path = new ResPath("/Config/client_config.toml");

            mgr.RegisterCVar("test.client", 0, CVar.ARCHIVE);
            mgr.RegisterCVar("test.server", 0, CVar.ARCHIVE | CVar.SERVER);
            mgr.RegisterCVar("test.server_only", 0, CVar.ARCHIVE | CVar.SERVERONLY);
            mgr.SetCVar("test.client", 1);
            mgr.SetCVar("test.server", 2, force: true);
            mgr.SetSaveFile(userData, path);
            mgr.SaveToFile();

            var text = userData.ReadAllText(path);
            Assert.That(text, Does.Contain("client = 1"));
            Assert.That(text, Does.Not.Contain("server = 2"));
            Assert.That(text, Does.Not.Contain("server_only"));
        }

        [Test]
        public void TestForkCVarsSaveToForkConfig()
        {
            var mgr = MakeCfgInternal(isServer: false);
            var userData = new VirtualWritableDirProvider();
            var configPath = new ResPath("/Config/client_config.toml");
            var forkConfigPath = new ResPath("/Config/test-fork_config.toml");

            mgr.RegisterCVar(CVars.BuildForkId.Name, "", CVar.NONE);
            mgr.LoadCVarsFromType(typeof(TestForkCVars));
            mgr.SetCVar(CVars.BuildForkId.Name, "test-fork");
            mgr.SetCVar(TestForkCVars.Shared.Name, 5);
            mgr.SetCVar(TestForkCVars.ForkSpecific.Name, 7);
            mgr.SetSaveFile(userData, configPath);
            mgr.SetForkSaveFile(userData, forkConfigPath);
            mgr.SaveToFile();

            var sharedText = userData.ReadAllText(configPath);
            var forkText = userData.ReadAllText(forkConfigPath);
            Assert.That(sharedText, Does.Contain("shared = 5"));
            Assert.That(sharedText, Does.Not.Contain("fork_specific"));
            Assert.That(forkText, Does.Contain("fork_specific = 7"));
            Assert.That(forkText, Does.Not.Contain("shared"));
        }

        [Test]
        public void TestForkCVarsSaveToUnspecifiedForkConfig()
        {
            var mgr = MakeCfgInternal(isServer: false);
            var userData = new VirtualWritableDirProvider();
            var configPath = new ResPath("/Config/client_config.toml");
            var forkConfigPath = new ResPath("/Config/unspecified_config.toml");

            mgr.LoadCVarsFromType(typeof(TestForkCVars));
            mgr.SetCVar(TestForkCVars.ForkSpecific.Name, 7);
            mgr.SetSaveFile(userData, configPath);
            mgr.SetForkSaveFile(userData, forkConfigPath);
            mgr.SaveToFile();

            var sharedText = userData.ReadAllText(configPath);
            var forkText = userData.ReadAllText(forkConfigPath);
            Assert.That(sharedText, Does.Not.Contain("fork_specific"));
            Assert.That(forkText, Does.Contain("fork_specific = 7"));
        }

        [Test]
        public void TestUserDataConfigSaveRejectsTraversal()
        {
            var mgr = MakeCfgInternal(isServer: false);
            var userData = new VirtualWritableDirProvider();

            Assert.That(
                () => mgr.SetSaveFile(userData, new ResPath("/Config/../client_config.toml")),
                Throws.ArgumentException);

            Assert.That(
                () => mgr.SetForkSaveFile(userData, new ResPath("/Config/../fork_config.toml")),
                Throws.ArgumentException);
        }

        private IConfigurationManager MakeCfg()
        {
            return MakeCfgInternal(isServer: true);
        }

        private ConfigurationManager MakeCfgInternal(bool isServer)
        {
            var collection = new DependencyCollection();
            collection.RegisterInstance<IReplayRecordingManager>(new Mock<IReplayRecordingManager>().Object);
            collection.RegisterInstance<INetManager>(new Mock<INetManager>().Object);
            collection.Register<ConfigurationManager, ConfigurationManager>();
            collection.Register<IConfigurationManager, ConfigurationManager>();
            collection.Register<IGameTiming, GameTiming>();
            collection.Register<ILogManager, LogManager>();
            collection.BuildGraph();

            var cfg = collection.Resolve<ConfigurationManager>();
            cfg.Initialize(isServer);
            return cfg;
        }

        [CVarDefs]
        private static class TestForkCVars
        {
            public static readonly CVarDef<int> Shared =
                CVarDef.Create("test.shared", 0, CVar.ARCHIVE);

            public static readonly CVarDef<int> ForkSpecific =
                CVarDef.Create("test.fork_specific", 0, CVar.ARCHIVE | CVar.FORK);
        }
    }
}
