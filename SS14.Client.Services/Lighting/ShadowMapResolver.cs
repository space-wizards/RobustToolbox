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
using System.Runtime.InteropServices.WindowsRuntime;
using SFML.System;
using SS14.Client.Graphics.OpenGL;
using SS14.Client.Graphics.Sprite;

namespace SS14.Client.Services.Lighting
{
    public class ShadowMapResolver : IDisposable
    {
        private readonly IResourceManager _resourceManager;
        private readonly int baseSize;
    
        private readonly int reductionChainCount;

        private int depthBufferSize;

        private RenderImage distancesRT;
        private RenderImage distortRT;
        private RenderImage processedShadowsRT;
        private RenderImage shadowMap;
        private RenderImage shadowsRT;    
        private RenderImage debugRt;    
        private RenderImage[] reductionRT;

        private TechniqueList resolveShadowsEffectTechnique;
        private TechniqueList reductionEffectTechnique;


        public ShadowMapResolver(ShadowmapSize maxShadowmapSize, ShadowmapSize maxDepthBufferSize,
                                 IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
         

            reductionChainCount = (int) maxShadowmapSize;
            baseSize = 2 << reductionChainCount;
            depthBufferSize = 2 << (int) maxDepthBufferSize;
        }

        public void LoadContent()
        {
            reductionEffectTechnique = _resourceManager.GetTechnique("reductionEffect");
            resolveShadowsEffectTechnique = _resourceManager.GetTechnique("resolveShadowsEffect"); 
             
            //// BUFFER TYPES ARE VERY IMPORTANT HERE AND IT WILL BREAK IF YOU CHANGE THEM!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! HONK HONK 
            //these work fine
            distortRT = new RenderImage("distortRT" + baseSize, baseSize, baseSize, ImageBufferFormats.BufferGR1616F);
            distancesRT = new RenderImage("distancesRT" + baseSize, baseSize, baseSize, ImageBufferFormats.BufferGR1616F);

            //these need the buffer format
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

        public void ResolveShadows(LightArea Area, bool attenuateShadows, Texture mask = null)
        {
            Texture shadowCastersTexture = Area.RenderTarget.Texture;
            RenderImage Result = Area.RenderTarget;
            SFML.System.Vector2f LightPosition = Area.LightPosition;
            Texture MaskTexture = mask == null ? Area.Mask.Texture : mask;
            Vector4f MaskProps = Vector4f.Zero;
            Vector4f diffuseColor = Vector4f.One;

            //Debug.DebugRendertarget(Area.RenderTarget);
            ExecuteTechnique(Area.RenderTarget, distancesRT, "ComputeDistances");
            //Debug.DebugRendertarget(distancesRT);
            ExecuteTechnique(distancesRT, distortRT, "Distort");
            //Debug.DebugRendertarget(distortRT);

            // Working now
            ApplyHorizontalReduction(distortRT, shadowMap);

            //only DrawShadows needs these vars
            resolveShadowsEffectTechnique["DrawShadows"].SetParameter("AttenuateShadows", attenuateShadows ? 0 : 1);
            resolveShadowsEffectTechnique["DrawShadows"].SetParameter("MaskProps", MaskProps);
            resolveShadowsEffectTechnique["DrawShadows"].SetParameter("DiffuseColor", diffuseColor);

            var maskSize = MaskTexture.Size;
            RenderImage MaskTarget = new RenderImage("MaskTarget", maskSize.X, maskSize.Y);
            //Debug.DebugRendertarget(shadowMap, "ShadowMap");
            ExecuteTechnique(MaskTarget, Result, "DrawShadows", shadowMap);
            //Debug.DebugRendertarget(Result, "DrawShadowsResult");

            resolveShadowsEffectTechnique["DrawShadows"].ResetCurrentShader();
        }

        private void DebugTex(RenderImage src, RenderImage dst)
        {
            CluwneLib.ResetShader();
            dst.BeginDrawing();
            src.Blit(0, 0, dst.Width, dst.Height, BlitterSizeMode.Scale);
            dst.EndDrawing();
        }

        private void ExecuteTechnique(RenderImage source, RenderImage destination, string techniqueName)
        {
            ExecuteTechnique(source, destination, techniqueName, null);
        }

        private void ExecuteTechnique(RenderImage source, RenderImage destinationTarget, string techniqueName, RenderImage shadowMap)
        {
            Vector2f renderTargetSize;
            renderTargetSize = new Vector2f(baseSize, baseSize);

            destinationTarget.BeginDrawing();
            destinationTarget.Clear(Color.White);

            resolveShadowsEffectTechnique[techniqueName].setAsCurrentShader() ;

            resolveShadowsEffectTechnique[techniqueName].SetParameter("renderTargetSize", renderTargetSize);
            if (source != null)
                resolveShadowsEffectTechnique[techniqueName].SetParameter("inputSampler", source);
            if (shadowMap != null)
                resolveShadowsEffectTechnique[techniqueName].SetParameter("shadowMapSampler", shadowMap);

            // Blit and use normal sampler instead of doing that weird InputTexture bullshit
            // Use destination width/height otherwise you can see some cropping result erroneously.
            source.Blit(0, 0, destinationTarget.Width, destinationTarget.Height, BlitterSizeMode.Scale);

            destinationTarget.EndDrawing();
        }



        private void ApplyHorizontalReduction(RenderImage source, RenderImage destination)
        {
            int step = reductionChainCount - 1;
            RenderImage src = source;
            RenderImage HorizontalReduction= reductionRT[step];
            reductionEffectTechnique["HorizontalReduction"].setAsCurrentShader();
            // Disabled GLTexture stuff for now just to get the pipeline working.
            // The only side effect is that floating point precision will be low,
            // making light borders and shit have jaggy edges.
            //GLTexture GLHorizontalReduction = new GLTexture("desto", (int)source.Width, (int)source.Height, ImageBufferFormats.BufferGR1616F);
            //Debug.DebugRendertarget(source);
            while (step >= 0)
            {
                HorizontalReduction = reductionRT[step]; // next step

                HorizontalReduction.BeginDrawing();
                HorizontalReduction.Clear(Color.White);
            
                //reductionEffectTechnique["HorizontalReduction"].SetParameter("secondTexture", src);
                reductionEffectTechnique["HorizontalReduction"].SetParameter("TextureDimensions",1.0f/src.Width);

                // Sourcetexture not needed... just blit!
                src.Blit(0, 0, HorizontalReduction.Width, HorizontalReduction.Height, BlitterSizeMode.Scale); // draw SRC to HR   
                                                                                                              //fix
                                                                                                              //GLHorizontalReduction.Blit(src.Texture, CluwneLib.CurrentShader);

                HorizontalReduction.EndDrawing();
                src = HorizontalReduction; // hr becomes new src 
                //Debug.DebugRendertarget(HorizontalReduction);
                step--;
            }


            CluwneLib.ResetShader();
            //copy to destination
            destination.BeginDrawing();
            destination.Clear(Color.White);

            HorizontalReduction.Blit(0, 0, destination.Height, destination.Width);
                //GLHorizontalReduction.Blit(HorizontalReduction.Texture, CluwneLib.CurrentShader); 
            destination.EndDrawing();
            //Debug.DebugRendertarget(destination);
            CluwneLib.ResetRenderTarget();
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