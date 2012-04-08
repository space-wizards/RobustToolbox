using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using ClientInterfaces.Resource;

namespace SS3D.LightTest
{
    enum ShadowmapSize
    {
        Size128 = 6,
        Size256 = 7,
        Size512 = 8,
        Size1024 = 9,
    }
    class ShadowMapResolver
    {
        private readonly IResourceManager _resourceManager;
        
        private int reductionChainCount;
        private int baseSize;
        private int depthBufferSize;

        FXShader resolveShadowsEffect;
        FXShader reductionEffect;

        RenderImage distortRT;
        RenderImage shadowMap;
        RenderImage shadowsRT;
        RenderImage processedShadowsRT;

        QuadRenderer quadRender;
        RenderImage distancesRT;
        RenderImage[] reductionRT;

        public ShadowMapResolver(QuadRenderer quad, ShadowmapSize maxShadowmapSize, ShadowmapSize maxDepthBufferSize, IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            this.quadRender = quad;

            reductionChainCount = (int)maxShadowmapSize;
            baseSize = 2 << reductionChainCount;
            depthBufferSize = 2 << (int)maxDepthBufferSize;
        }

        public void LoadContent()
        {
            reductionEffect = _resourceManager.GetShader("reductionEffect");
            resolveShadowsEffect = _resourceManager.GetShader("resolveShadowsEffect");

            // BUFFER TYPES ARE VERY IMPORTANT HERE AND IT WILL BREAK IF YOU CHANGE THEM!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            distortRT = new RenderImage("distortRT" + baseSize, baseSize, baseSize, ImageBufferFormats.BufferGR1616F);
            distancesRT = new RenderImage("distancesRT" + baseSize, baseSize, baseSize, ImageBufferFormats.BufferGR1616F);
            shadowMap = new RenderImage("shadowMap" + baseSize, 2, baseSize, ImageBufferFormats.BufferGR1616F);
            reductionRT = new RenderImage[reductionChainCount];
            for (int i = 0; i < reductionChainCount; i++)
            {
                reductionRT[i] = new RenderImage("reductionRT" + i + baseSize, 2 << i, baseSize, ImageBufferFormats.BufferGR1616F);
            }
            shadowsRT = new RenderImage("shadowsRT" + baseSize, baseSize, baseSize, ImageBufferFormats.BufferRGB888A8);
            processedShadowsRT = new RenderImage("processedShadowsRT" + baseSize, baseSize, baseSize, ImageBufferFormats.BufferRGB888A8);
        }

        public void ResolveShadows(Image shadowCastersTexture, RenderImage result, Vector2D lightPosition)
        {
            Gorgon.CurrentRenderTarget.BlendingMode = BlendingModes.None;
            ExecuteTechnique(shadowCastersTexture, distancesRT, "ComputeDistances");
            ExecuteTechnique(distancesRT.Image, distortRT, "Distort");
            ApplyHorizontalReduction(distortRT, shadowMap);
            ExecuteTechnique(null, shadowsRT, "DrawShadows", shadowMap);
            ExecuteTechnique(shadowsRT.Image, processedShadowsRT, "BlurHorizontally");
            ExecuteTechnique(processedShadowsRT.Image, result, "BlurVerticallyAndAttenuate");
            Gorgon.CurrentShader = null;
        }

        private void ExecuteTechnique(Image source, RenderImage destination, string techniqueName)
        {
            ExecuteTechnique(source, destination, techniqueName, null);
        }

        private void ExecuteTechnique(Image source, RenderImage destination, string techniqueName, RenderImage shadowMap)
        {
            Vector2D renderTargetSize;
            renderTargetSize = new Vector2D((float)baseSize, (float)baseSize);
            Gorgon.CurrentRenderTarget = destination;
            Gorgon.CurrentRenderTarget.Clear(System.Drawing.Color.White);

            Gorgon.CurrentShader = resolveShadowsEffect.Techniques[techniqueName];
            resolveShadowsEffect.Parameters["renderTargetSize"].SetValue(renderTargetSize);
            if (source != null)
                resolveShadowsEffect.Parameters["InputTexture"].SetValue(source);
            if (shadowMap != null)
                resolveShadowsEffect.Parameters["ShadowMapTexture"].SetValue(shadowMap);

            quadRender.Render(new Vector2D(1, 1) * -1, new Vector2D(1, 1));

            Gorgon.CurrentRenderTarget = null;
        }

        private void ApplyHorizontalReduction(RenderImage source, RenderImage destination)
        {
            int step = reductionChainCount - 1;
            RenderImage s = source;
            RenderImage d = reductionRT[step];
            Gorgon.CurrentShader = reductionEffect.Techniques["HorizontalReduction"];

            while (step >= 0)
            {
                d = reductionRT[step];

                Gorgon.CurrentRenderTarget = d;
                d.Clear(System.Drawing.Color.White);

                reductionEffect.Parameters["SourceTexture"].SetValue(s);
                Vector2D textureDim = new Vector2D(1.0f / (float)s.Width, 1.0f / (float)s.Height);
                reductionEffect.Parameters["TextureDimensions"].SetValue(textureDim);
                quadRender.Render(new Vector2D(1, 1) * -1, new Vector2D(1, 1));
                s = d;
                step--;

            }

            //copy to destination
            Gorgon.CurrentRenderTarget = destination;
            Gorgon.CurrentShader = reductionEffect.Techniques["Copy"];
            reductionEffect.Parameters["SourceTexture"].SetValue(d);
            Gorgon.CurrentRenderTarget.Clear(System.Drawing.Color.White);
            quadRender.Render(new Vector2D(1, 1) * -1, new Vector2D(1, 1));

            reductionEffect.Parameters["SourceTexture"].SetValue(reductionRT[reductionChainCount - 1]);
            Gorgon.CurrentRenderTarget = null;
        }

    }
}
