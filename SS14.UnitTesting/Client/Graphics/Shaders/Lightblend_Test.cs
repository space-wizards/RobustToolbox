using NUnit.Framework;

namespace SS14.UnitTesting.SS14.Client.Graphics.Shaders
{
    [TestFixture]
    [Ignore("This test does nothing.")]
    [Category("rendering")]
    public class Lightblend_Test : SS14UnitTest
    {
        public override bool NeedsClientConfig => true;
        public override bool NeedsResourcePack => true;
        public override UnitTestProject Project => UnitTestProject.Client;

        [Test]
        public void LightBlend_Test()
        {

        }
    }
}
