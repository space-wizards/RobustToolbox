using SS14.Client.Graphics;
using SS14.Client.Graphics.Shader;
using SS14.Client.Graphics.Render;
using SS14.Shared.Maths;
using SS14.Client.Interfaces.Resource;
using System;
using System.Drawing;

namespace SS14.Client.Services.Lighting
{
    public class ShadowMapResolver : IDisposable
    {
        private readonly IResourceManager _resourceManager;

        private readonly int baseSize;
        private readonly QuadRenderer quadRender;
        private readonly int reductionChainCount;
        private int depthBufferSize;
        private RenderImage distancesRT;

        private RenderImage distortRT;
        private RenderImage processedShadowsRT;
        private FXShader reductionEffect;

        private RenderImage[] reductionRT;
        private FXShader resolveShadowsEffect;
        private RenderImage shadowMap;
        private RenderImage shadowsRT;

        public ShadowMapResolver(QuadRenderer quad, ShadowmapSize maxShadowmapSize, ShadowmapSize maxDepthBufferSize,
                                 IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            quadRender = quad;

            //reductionChainCount =  maxShadowmapSize;
            //baseSize = 2 << reductionChainCount;
            //depthBufferSize = 2 << maxDepthBufferSize;
        }

        public void LoadContent()
        {
            reductionEffect = _resourceManager.GetShader("reductionEffect");
            resolveShadowsEffect = _resourceManager.GetShader("resolveShadowsEffect");

            //// BUFFER TYPES ARE VERY IMPORTANT HERE AND IT WILL BREAK IF YOU CHANGE THEM!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            //distortRT = new RenderImage("distortRT" + baseSize, baseSize, baseSize, ImageBufferFormats.BufferGR1616F);
            //distancesRT = new RenderImage("distancesRT" + baseSize, baseSize, baseSize, ImageBufferFormats.BufferGR1616F);
            //shadowMap = new RenderImage("shadowMap" + baseSize, 2, baseSize, ImageBufferFormats.BufferGR1616F);
            reductionRT = new RenderImage[reductionChainCount];
            for (int i = 0; i < reductionChainCount; i++)
            {
                reductionRT[i] = new RenderImage("reductionRT" + i + baseSize, 2 << i, baseSize,
                                                 ImageBufferFormats.BufferGR1616F);
            }
            shadowsRT = new RenderImage("shadowsRT" + baseSize, baseSize, baseSize, ImageBufferFormats.BufferRGB888A8);
            processedShadowsRT = new RenderImage("processedShadowsRT" + baseSize, baseSize, baseSize,
                                                 ImageBufferFormats.BufferRGB888A8);
        }

        public void ResolveShadows(Image shadowCastersTexture, RenderImage result, Vector2 lightPosition,
                                   bool attenuateShadows, Image mask, Vector4 maskProps, Vector4 diffuseColor)
        {
            //resolveShadowsEffect.Parameters["AttenuateShadows"].SetValue(attenuateShadows ? 0 : 1);
            //resolveShadowsEffect.Parameters["MaskProps"].SetValue(maskProps);
            //resolveShadowsEffect.Parameters["DiffuseColor"].SetValue(diffuseColor);
          //  CluwneLib.CurrentRenderTarget.BlendingMode = BlendingModes.None;
            ExecuteTechnique(shadowCastersTexture, distancesRT, "ComputeDistances");
          //  ExecuteTechnique(distancesRT.Image, distortRT, "Distort");
            ApplyHorizontalReduction(distortRT, shadowMap);
            ExecuteTechnique(mask, result, "DrawShadows", shadowMap);
            //ExecuteTechnique(shadowsRT.Image, processedShadowsRT, "BlurHorizontally");
            //ExecuteTechnique(processedShadowsRT.Image, result, "BlurVerticallyAndAttenuate");
            CluwneLib.CurrentShader = null;
        }

        private void ExecuteTechnique(Image source, RenderImage destination, string techniqueName)
        {
            ExecuteTechnique(source, destination, techniqueName, null);
        }

        private void ExecuteTechnique(Image source, RenderImage destination, string techniqueName, RenderImage shadowMap)
        {
            Vector2 renderTargetSize;
            renderTargetSize = new Vector2(baseSize, baseSize);
            CluwneLib.CurrentRenderTarget = destination;
            CluwneLib.CurrentRenderTarget.Clear(CluwneLib.SystemColorToSFML(Color.White));

           //CluwneLib.CurrentShader = resolveShadowsEffect.Techniques[techniqueName];
           // resolveShadowsEffect.Parameters["renderTargetSize"].SetValue(renderTargetSize);
           // if (source != null)
           //     resolveShadowsEffect.Parameters["InputTexture"].SetValue(source);
           // if (shadowMap != null)
           //     resolveShadowsEffect.Parameters["ShadowMapTexture"].SetValue(shadowMap);

            quadRender.Render(new Vector2(1, 1)*-1, new Vector2(1, 1));

            CluwneLib.CurrentRenderTarget = null;
        }

        private void ApplyHorizontalReduction(RenderImage source, RenderImage destination)
        {
            int step = reductionChainCount - 1;
            RenderImage s = source;
            RenderImage d = reductionRT[step];
          //  CluwneLib.CurrentShader = reductionEffect.Techniques["HorizontalReduction"];

            while (step >= 0)
            {
                d = reductionRT[step];

                CluwneLib.CurrentRenderTarget = d;
                d.Clear(Color.White);

             //   reductionEffect.Parameters["SourceTexture"].SetValue(s);
                var textureDim = new Vector2(1.0f/s.Width, 1.0f/s.Height);
           //     reductionEffect.Parameters["TextureDimensions"].SetValue(textureDim);
                quadRender.Render(new Vector2(1, 1)*-1, new Vector2(1, 1));
                s = d;
                step--;
            }

            //copy to destination
            CluwneLib.CurrentRenderTarget = destination;
           // CluwneLib.CurrentShader = reductionEffect.Techniques["Copy"];
      //      reductionEffect.Parameters["SourceTexture"].SetValue(d);
            CluwneLib.CurrentRenderTarget.Clear(CluwneLib.SystemColorToSFML(Color.White));
            quadRender.Render(new Vector2(1, 1)*-1, new Vector2(1, 1));

       //     reductionEffect.Parameters["SourceTexture"].SetValue(reductionRT[reductionChainCount - 1]);
            CluwneLib.CurrentRenderTarget = null;
        }

        public void Dispose()
        {
          //  distancesRT.ForceRelease();
            distancesRT.Dispose();
         //   distortRT.ForceRelease();
            distortRT.Dispose();
         //   processedShadowsRT.ForceRelease();
            processedShadowsRT.Dispose();
            foreach(var rt in reductionRT)
            {
           //     rt.ForceRelease();
                rt.Dispose();
            }
          //  shadowMap.ForceRelease();
            shadowMap.Dispose();
         //   shadowsRT.ForceRelease();
            shadowsRT.Dispose();
        }
    }
}