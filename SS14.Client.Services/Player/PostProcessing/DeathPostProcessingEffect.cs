using SS14.Client.Graphics.Shader;
using SS14.Client.Graphics.Render;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.IoC;
using System;
using System.Drawing;
using SS14.Client.Graphics;


namespace SS14.Client.Services.Player.PostProcessing
{
    public class DeathPostProcessingEffect : PostProcessingEffect
    {
        private readonly GLSLShader _shader;

        public DeathPostProcessingEffect(float duration): base(duration)
        {
            _shader = IoCManager.Resolve<IResourceManager>().GetShader("deathshader");
        }

        public override void ProcessImage(RenderImage image)
        {
            var OstafLikesTheCock = new RenderImage(image.Height, image.Height);

            CluwneLib.CurrentRenderTarget = OstafLikesTheCock;

            image.Blit(0, 0, image.Height, image.Height, Color.White, BlitterSizeMode.Crop);
            CluwneLib.CurrentRenderTarget = image;
            CluwneLib.CurrentShader = _shader;
            _shader.SetParameter("SceneTexture", OstafLikesTheCock);
            _shader.setDuration((Math.Abs(_duration)));
            OstafLikesTheCock.Blit(0, 0, image.Height, image.Height, Color.White, BlitterSizeMode.Crop);

            CluwneLib.CurrentRenderTarget = null;
            CluwneLib.CurrentShader = null;
            OstafLikesTheCock.Dispose();
        }
    }
}