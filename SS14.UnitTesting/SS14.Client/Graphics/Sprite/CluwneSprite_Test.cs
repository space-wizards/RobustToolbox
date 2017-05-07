#if !HEADLESS
using NUnit.Framework;
using SFML.Graphics;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Render;
using SS14.Client.Interfaces.Resource;

namespace SS14.UnitTesting.SS14.Client.Graphics.Sprite
{
    [TestFixture]
    public class CluwneSprite_Test : SS14UnitTest
    {

        private IResourceManager resources;
        private RenderImage test;

        public CluwneSprite_Test()
        {
            resources = base.GetResourceManager;

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
#endif
