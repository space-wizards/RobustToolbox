using NUnit.Framework;
using System;

namespace SS14.UnitTesting.SS14.Client.Player.PostProcessing
{
    [TestFixture]
    [Ignore("This test does nothing.")]
    [Category("rendering")]
    public class DeathPostProcessingEffect_Test : SS14UnitTest
    {
        public override bool NeedsClientConfig => true;
        public override bool NeedsResourcePack => true;
        public override UnitTestProject Project => UnitTestProject.Client;
        public DeathPostProcessingEffect_Test()
        {

        }

        [Test]
        public void TestDeathPostProcessEffect_DoesSomething()
        {
            Assert.IsTrue(true);
        }
    }
}
