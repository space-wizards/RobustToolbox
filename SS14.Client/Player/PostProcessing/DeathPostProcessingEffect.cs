/*
using SS14.Client.Graphics;
using SS14.Client.Graphics.Render;
using SS14.Client.Graphics.Shader;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using System;

namespace SS14.Client.Player.PostProcessing
{
    public class DeathPostProcessingEffect : PostProcessingEffect
    {
        private readonly GLSLShader _shader;

        public DeathPostProcessingEffect(float duration) : base(duration)
        {
            _shader = IoCManager.Resolve<IResourceCache>().GetShader("deathshader");
        }

        public override void ProcessImage(RenderImage image)
        {
            var OstafLikesTheCock = new RenderImage("CockLoverOstaf", image.Height, image.Height);

            OstafLikesTheCock.BeginDrawing();
            image.Blit(0, 0, image.Height, image.Height, Color.White, BlitterSizeMode.Crop);
            OstafLikesTheCock.EndDrawing();

            image.BeginDrawing();
            _shader.setAsCurrentShader();
            _shader.SetUniform("SceneTexture", OstafLikesTheCock);
            _shader.setDuration((Math.Abs(_duration)));
            OstafLikesTheCock.Blit(0, 0, image.Height, image.Height, Color.White, BlitterSizeMode.Crop);
            image.EndDrawing();

            _shader.ResetCurrentShader();
            OstafLikesTheCock.Dispose();
        }
    }
}
*/
