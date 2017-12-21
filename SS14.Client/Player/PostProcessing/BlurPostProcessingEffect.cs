/*
using OpenTK;
using SS14.Client.Graphics.Render;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Helpers;
using SS14.Shared.IoC;
using System;

namespace SS14.Client.Player.PostProcessing
{
    public class BlurPostProcessingEffect : PostProcessingEffect
    {
        private readonly GaussianBlur _gaussianBlur = new GaussianBlur(IoCManager.Resolve<IResourceCache>());

        public BlurPostProcessingEffect(float duration)
            : base(duration)
        {
        }

        public override void ProcessImage(RenderImage image)
        {
            if (_duration < 3)
                _gaussianBlur.SetRadius(3);
            else if (_duration < 10)
                _gaussianBlur.SetRadius(5);
            else
                _gaussianBlur.SetRadius(7);

            _gaussianBlur.SetSize(new Vector2(image.Height, image.Height));
            _gaussianBlur.SetAmount(Math.Min(_duration / 2, 3f));
            _gaussianBlur.PerformGaussianBlur(image);
        }
    }
}
*/
