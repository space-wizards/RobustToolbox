using NUnit.Framework;
using SFML.Graphics;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Render;
using SS14.Client.Graphics.Shader;
using SS14.Client.Interfaces.Resource;

namespace SS14.UnitTesting.SS14.Client.Graphics.Shaders
{
    [TestFixture, Explicit]
    [Category("rendering")]
    public class TestShader_Test : SS14UnitTest
    {
        public override bool NeedsClientConfig => true;
        public override bool NeedsResourcePack => true;

        private IResourceManager resources;
        private RenderImage testRenderImage;
        private SFML.Graphics.Sprite testsprite;

        public TestShader_Test()
        {
            base.InitializeCluwneLib(1280,720,false,60);

            resources = base.GetResourceManager;
            testRenderImage = new RenderImage("TestShaders",1280,720);
            testsprite = resources.GetSprite("flashlight_mask");

            SS14UnitTest.InjectedMethod += LoadTestShader_ShouldDrawAllRed;

            base.StartCluwneLibLoop();

        }

        [Test]
        public void LoadTestShader_ShouldDrawAllRed()
        {

            testRenderImage.BeginDrawing();


            GLSLShader currshader = resources.GetShader("RedShader");
            currshader.SetParameter("TextureUnit0", testsprite.Texture);
            currshader.setAsCurrentShader();
            testsprite.Draw();
            testRenderImage.EndDrawing();
            currshader.ResetCurrentShader();
            testRenderImage.Blit(0, 0, 1280, 720, Color.White, BlitterSizeMode.Crop);

            resources.GetSprite("flashlight_mask").Draw();


        }



    }
}

