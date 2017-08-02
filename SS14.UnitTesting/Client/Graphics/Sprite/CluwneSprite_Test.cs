using NUnit.Framework;
using SFML.Graphics;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Render;
using SS14.Client.Interfaces.Resource;

namespace SS14.UnitTesting.SS14.Client.Graphics.Sprite
{
    [TestFixture, Explicit]
    [Category("rendering")]
    public class CluwneSprite_Test : SS14UnitTest
    {
        public override bool NeedsClientConfig => true;
        public override bool NeedsResourcePack => true;
        public override UnitTestProject Project => UnitTestProject.Client;

        private IResourceCache resources;
        private RenderImage test;

        [OneTimeSetUp]
        public void Setup()
        {
            resources = base.GetResourceCache;

            base.InitializeCluwneLib();

            SS14UnitTest.InjectedMethod += DefaultDrawMethod_ShouldDrawToScreen;
            test = new RenderImage("testtst", 1280, 720);
            base.StartCluwneLibLoop();

        }

        [Test]
        public void DefaultDrawMethod_ShouldDrawToScreen()
        {
            test.BeginDrawing();
            test.Clear(Color.Black);
            resources.GetSprite("flashlight_mask").Draw();
            test.EndDrawing();
            test.Blit(0, 0, 1280, 720, Color.White, BlitterSizeMode.Scale);
        }
    }
}
