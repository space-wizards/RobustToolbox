using NUnit.Framework;
using Robust.Shared.Configuration;

namespace Robust.UnitTesting.Shared.Configuration
{
    internal sealed class ConfigurationManagerTest
    {
        [Test]
        public void TestSecureCVar()
        {
            var cfg = new ConfigurationManager();

            cfg.RegisterCVar("auth.token", "honk", CVar.SECURE);

            Assert.That(() => cfg.GetCVar<string>("auth.token"), Throws.TypeOf<InvalidConfigurationException>());
            Assert.That(() => cfg.GetCVarType("auth.token"), Throws.TypeOf<InvalidConfigurationException>());
            Assert.That(cfg.GetSecureCVar<string>("auth.token"), Is.EqualTo("honk"));
            Assert.That(cfg.IsCVarRegistered("auth.token"), Is.False);
            Assert.That(cfg.GetRegisteredCVars(), Does.Not.Contain("auth.token"));
        }
    }
}
