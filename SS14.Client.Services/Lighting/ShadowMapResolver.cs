using SS14.Client.Graphics;
using SS14.Client.Graphics.Shader;
using SS14.Client.Graphics.Render;
using SS14.Shared.Maths;
using SS14.Client.Interfaces.Resource;
using System;
using System.Drawing;
using SFML.Graphics;
using Color = SFML.Graphics.Color;
using System.Collections.Generic;

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
        private RenderImage shadowMap;
        private RenderImage shadowsRT;    
        private RenderImage[] reductionRT;

        private TechniqueList resolveShadowsEffectTechnique;
        private TechniqueList reductionEffectTechnique;
        

        public ShadowMapResolver(QuadRenderer quad, ShadowmapSize maxShadowmapSize, ShadowmapSize maxDepthBufferSize,
                                 IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            quadRender = quad;

            reductionChainCount = (int) maxShadowmapSize;
            baseSize = 2 << reductionChainCount;
            depthBufferSize = 2 << (int) maxDepthBufferSize;

        }

        public void LoadContent()
        {

            reductionEffectTechnique = _resourceManager.GetTechnique("reductionEffect");
            resolveShadowsEffectTechnique = _resourceManager.GetTechnique("resolveShadowsEffect"); 
             
            //// BUFFER TYPES ARE VERY IMPORTANT HERE AND IT WILL BREAK IF YOU CHANGE THEM!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! HONK HONK 
            distortRT = new RenderImage("distortRT" + baseSize, baseSize, baseSize, ImageBufferFormats.BufferGR1616F);
            distancesRT = new RenderImage("distancesRT" + baseSize, baseSize, baseSize, ImageBufferFormats.BufferGR1616F);
            shadowMap = new RenderImage("shadowMap" + baseSize, 2, baseSize, ImageBufferFormats.BufferGR1616F);
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

        public void ResolveShadows(Texture shadowCastersTexture, RenderImage result, Vector2 lightPosition,
                                   bool attenuateShadows, Texture mask, Vector4 maskProps, Vector4 diffuseColor)
        {
            //only DrawShadows needs these vars
            resolveShadowsEffectTechnique["DrawShadows"].SetParameter("AttenuateShadows", attenuateShadows ? 0 : 1);
            resolveShadowsEffectTechnique["DrawShadows"].SetParameter("MaskProps", maskProps);
            resolveShadowsEffectTechnique["DrawShadows"].SetParameter("DiffuseColor", diffuseColor);

          // CluwneLib.CurrentRenderTarget.BlendingMode = BlendingModes.None;
            ExecuteTechnique(shadowCastersTexture, distancesRT, "ComputeDistances");
            ExecuteTechnique(distancesRT.Texture, distortRT, "Distort");
            ApplyHorizontalReduction(distortRT, shadowMap);
            ExecuteTechnique(mask, result, "DrawShadows", shadowMap);
            ExecuteTechnique(shadowsRT.Texture, processedShadowsRT, "BlurHorizontally");
            ExecuteTechnique(processedShadowsRT.Texture, result, "BlurVerticallyAndAttenuate");
            CluwneLib.CurrentShader = null;
        }

        private void ExecuteTechnique(Texture source, RenderImage destination, string techniqueName)
        {
            ExecuteTechnique(source, destination, techniqueName, null);
        }

        private void ExecuteTechnique(Texture source, RenderImage destination, string techniqueName, RenderImage shadowMap)
        {
            Vector2 renderTargetSize;
            renderTargetSize = new Vector2(baseSize, baseSize);
          
            CluwneLib.CurrentRenderTarget = destination;
            CluwneLib.CurrentRenderTarget.Clear(Color.White);

            CluwneLib.CurrentShader = resolveShadowsEffectTechnique[techniqueName];

                resolveShadowsEffectTechnique[techniqueName].SetParameter("renderTargetSize", renderTargetSize);
            if (source != null)
                resolveShadowsEffectTechnique[techniqueName].SetParameter("InputTexture", source);
            if (shadowMap != null)
                resolveShadowsEffectTechnique[techniqueName].SetParameter("ShadowMapTexture", shadowMap);

            quadRender.Render(new Vector2(1, 1)*-1, new Vector2(1, 1));

            CluwneLib.CurrentRenderTarget = null;
        }

        private void ApplyHorizontalReduction(RenderImage source, RenderImage destination)
        {
            int step = reductionChainCount - 1;
            RenderImage s = source;
            RenderImage d = reductionRT[step];
            CluwneLib.CurrentShader = reductionEffectTechnique["HorizontalReduction"];

            while (step >= 0)
            {
                d = reductionRT[step];

                CluwneLib.CurrentRenderTarget = d;
                d.Clear(Color.White);
            
                var textureDim = new Vector2(1.0f/s.Width, 1.0f/s.Height);
                reductionEffectTechnique["HorizontalReduction"].SetParameter("SourceTexture", s);
                reductionEffectTechnique["HorizontalReduction"].SetParameter("TextureDimensions",textureDim);
                quadRender.Render(new Vector2(1, 1)*-1, new Vector2(1, 1));
                s = d;
                step--;
            }

            //copy to destination
            CluwneLib.CurrentRenderTarget = destination;
           
            reductionEffectTechnique["Copy"].SetParameter("SourceTexture",d);
            reductionEffectTechnique["Copy"].SetParameter("SourceTexture", reductionRT[reductionChainCount - 1]);
            CluwneLib.CurrentShader = reductionEffectTechnique["Copy"];

            CluwneLib.CurrentRenderTarget.Clear(Color.White);
            quadRender.Render(new Vector2(1, 1)*-1, new Vector2(1, 1));

          
            CluwneLib.CurrentRenderTarget = null;
        }

        public void Dispose()
        {
          
            distancesRT.Dispose();
            distortRT.Dispose();
            processedShadowsRT.Dispose();
            foreach(var rt in reductionRT)
            {
           
                rt.Dispose();
            }
        
            shadowMap.Dispose();
            shadowsRT.Dispose();
        }
    }
}