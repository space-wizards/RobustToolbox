using System;
using System.Drawing;
using ClientInterfaces.Resource;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using SS13.IoC;

namespace ClientServices.Player.PostProcessing
{
    public class DeathPostProcessingEffect : PostProcessingEffect
    {
        private readonly FXShader _shader;

        public DeathPostProcessingEffect(float duration)
            : base(duration)
        {
            _shader = IoCManager.Resolve<IResourceManager>().GetShader("deathshader");
        }

        public override void ProcessImage(RenderImage image)
        {
            var OstafLikesTheCock = new RenderImage("OstafLikesTheCock", image.Width, image.Height,
                                                    ImageBufferFormats.BufferRGB888A8);
            Gorgon.CurrentRenderTarget = OstafLikesTheCock;
            image.Image.Blit(0, 0, image.Width, image.Height, Color.White, BlitterSizeMode.Crop);
            Gorgon.CurrentRenderTarget = image;
            Gorgon.CurrentShader = _shader.Techniques["DeathShader"];
            _shader.Parameters["SceneTexture"].SetValue(OstafLikesTheCock);
            _shader.Parameters["duration"].SetValue(Math.Abs(_duration));
            OstafLikesTheCock.Image.Blit(0, 0, image.Width, image.Height, Color.White, BlitterSizeMode.Crop);

            Gorgon.CurrentRenderTarget = null;
            Gorgon.CurrentShader = null;
            OstafLikesTheCock.Dispose();
        }
    }
}