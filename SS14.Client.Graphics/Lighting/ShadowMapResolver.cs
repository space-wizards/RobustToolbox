using System;
using SS14.Client.Graphics.Render;
using SS14.Client.Graphics.Shader;
using SS14.Shared.Maths;
using System.Collections.Generic;
using SS14.Client.Graphics.Sprites;

namespace SS14.Client.Graphics.Lighting
{
    public class ShadowMapResolver : IDisposable
    {
        private readonly int baseSize;

        private readonly int reductionChainCount;

        private int depthBufferSize;

        private RenderImage distancesRT;
        private RenderImage distortRT;
        private RenderImage processedShadowsRT;
        private TechniqueList reductionEffectTechnique;
        private RenderImage[] reductionRT;

        private TechniqueList resolveShadowsEffectTechnique;
        private RenderImage shadowMap;
        private RenderImage shadowsRT;

        private Dictionary<Vector2u, RenderImage> BlankRenderImages = new Dictionary<Vector2u, RenderImage>();

        public ShadowMapResolver(ShadowmapSize maxShadowmapSize, ShadowmapSize maxDepthBufferSize)
        {
            reductionChainCount = (int)maxShadowmapSize;
            baseSize = 2 << reductionChainCount;
            depthBufferSize = 2 << (int)maxDepthBufferSize;
        }

        public void Dispose()
        {
            distancesRT.Dispose();
            distortRT.Dispose();
            processedShadowsRT.Dispose();
            foreach (var rt in reductionRT)
            {
                rt.Dispose();
            }

            shadowMap.Dispose();
            shadowsRT.Dispose();
        }

        public void LoadContent(TechniqueList reduction, TechniqueList resolve)
        {
            reductionEffectTechnique = reduction;
            resolveShadowsEffectTechnique = resolve;

            //// BUFFER TYPES ARE VERY IMPORTANT HERE AND IT WILL BREAK IF YOU CHANGE THEM!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! HONK HONK
            //these work fine
            distortRT = new RenderImage("distortRT" + baseSize, baseSize, baseSize, ImageBufferFormats.BufferGR1616F);
            distancesRT = new RenderImage("distancesRT" + baseSize, baseSize, baseSize, ImageBufferFormats.BufferGR1616F);

            //these need the buffer format
            shadowMap = new RenderImage("shadowMap" + baseSize, 2, baseSize, ImageBufferFormats.BufferGR1616F);
            reductionRT = new RenderImage[reductionChainCount];
            for (var i = 0; i < reductionChainCount; i++)
            {
                reductionRT[i] = new RenderImage("reductionRT" + i + baseSize, 2 << i, baseSize,
                    ImageBufferFormats.BufferGR1616F);
            }
            shadowsRT = new RenderImage("shadowsRT" + baseSize, baseSize, baseSize, ImageBufferFormats.BufferRGB888A8);
            processedShadowsRT = new RenderImage("processedShadowsRT" + baseSize, baseSize, baseSize,
                ImageBufferFormats.BufferRGB888A8);
        }

        public void ResolveShadows(ILight light, bool attenuateShadows)
        {
            var lightRt = light.RenderTarget;
            var lightMask = light.Mask;
            var maskSize = lightMask.Size;
            var MaskProps = Vector4.Zero;
            var diffuseColor = Vector4.One;

            ExecuteTechnique(light.RenderTarget, distancesRT, "ComputeDistances", null);
            ExecuteTechnique(distancesRT, distortRT, "Distort", null);

            // Working now
            ApplyHorizontalReduction(distortRT, shadowMap);

            lightRt.BeginDrawing();
            {
                //clear lightRT
                lightRt.Clear(Color.Black);

                // draw light mask onto lightRT
                var sprite = new Sprite(lightMask);
                sprite.Scale = new Vector2((float)lightRt.Width / maskSize.X, (float)lightRt.Height / maskSize.Y);
                lightRt.Draw(sprite);
            }
            lightRt.EndDrawing();

            //only DrawShadows needs these vars
            resolveShadowsEffectTechnique["DrawShadows"].SetUniform("AttenuateShadows", attenuateShadows ? 0 : 1);
            resolveShadowsEffectTechnique["DrawShadows"].SetUniform("MaskProps", MaskProps);
            resolveShadowsEffectTechnique["DrawShadows"].SetUniform("DiffuseColor", diffuseColor);
            
            // I have TRIED to get this to work without this blank one.
            // I give the hell up.
            // I'll just cache them so they're not being recreated constantly.
            if (!BlankRenderImages.TryGetValue(maskSize, out var MaskTarget))
            {
                MaskTarget = new RenderImage("MaskTarget", maskSize.X, maskSize.Y);
                BlankRenderImages[maskSize] = MaskTarget;
            }
            ExecuteTechnique(MaskTarget, lightRt, "DrawShadows", shadowMap);

            resolveShadowsEffectTechnique["DrawShadows"].ResetCurrentShader();
        }

        private void DebugTex(RenderImage src, RenderImage dst)
        {
            CluwneLib.ResetShader();
            dst.BeginDrawing();
            src.Blit(0, 0, dst.Width, dst.Height, BlitterSizeMode.Scale);
            dst.EndDrawing();
        }

        private void ExecuteTechnique(RenderImage source, RenderImage destinationTarget, string techniqueName, RenderImage shadowMap)
        {
            Vector2 renderTargetSize;
            renderTargetSize = new Vector2(baseSize, baseSize);

            destinationTarget.BeginDrawing();

            if(shadowMap == null)
                destinationTarget.Clear(Color.White);

            resolveShadowsEffectTechnique[techniqueName].setAsCurrentShader();

            resolveShadowsEffectTechnique[techniqueName].SetUniform("renderTargetSize", renderTargetSize);
            resolveShadowsEffectTechnique[techniqueName].SetUniform("inputSampler", source);
            if (shadowMap != null)
                resolveShadowsEffectTechnique[techniqueName].SetUniform("shadowMapSampler", shadowMap);

            // Blit and use normal sampler instead of doing that weird InputTexture bullshit
            // Use destination width/height otherwise you can see some cropping result erroneously.
            source.Blit(0, 0, destinationTarget.Width, destinationTarget.Height, BlitterSizeMode.Scale);

            destinationTarget.EndDrawing();
        }

        private void ApplyHorizontalReduction(RenderImage source, RenderImage destination)
        {
            var step = reductionChainCount - 1;
            var src = source;
            var HorizontalReduction = reductionRT[step];
            reductionEffectTechnique["HorizontalReduction"].setAsCurrentShader();
            // Disabled GLTexture stuff for now just to get the pipeline working.
            // The only side effect is that floating point precision will be low,
            // making light borders and shit have jaggy edges.
            while (step >= 0)
            {
                HorizontalReduction = reductionRT[step]; // next step

                HorizontalReduction.BeginDrawing();
                HorizontalReduction.Clear(Color.White);

                reductionEffectTechnique["HorizontalReduction"].SetUniform("TextureDimensions", 1.0f / src.Width);

                // Sourcetexture not needed... just blit!
                src.Blit(0, 0, HorizontalReduction.Width, HorizontalReduction.Height, BlitterSizeMode.Scale); // draw SRC to HR
                //fix

                HorizontalReduction.EndDrawing();
                src = HorizontalReduction; // hr becomes new src
                step--;
            }

            CluwneLib.ResetShader();
            //copy to destination
            destination.BeginDrawing();
            destination.Clear(Color.White);

            HorizontalReduction.Blit(0, 0, destination.Height, destination.Width);
            destination.EndDrawing();
            CluwneLib.ResetRenderTarget();
        }
    }
}
