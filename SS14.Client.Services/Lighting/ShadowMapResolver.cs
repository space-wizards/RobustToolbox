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

        public void ResolveShadows(LightArea Area, bool attenuateShadows, Texture mask)
        {
            Texture shadowCastersTexture = Area.RenderTarget.Texture;
            RenderImage Result = Area.RenderTarget;
            Vector2 LightPosition = Area.LightPosition;
            Texture MaskTexture = Area.Mask.Texture;
            Vector4 MaskProps = Vector4.Zero;
            Vector4 diffuseColor = Vector4.One;

            ExecuteTechnique(Area.RenderTarget, distancesRT, "ComputeDistances");
            ExecuteTechnique(distancesRT, distortRT, "Distort");
            // These first 2 steps are working pretty much.
            
            // This next step needs fixing
            ApplyHorizontalReduction(distortRT, shadowMap);

            //only DrawShadows needs these vars
            resolveShadowsEffectTechnique["DrawShadows"].SetParameter("AttenuateShadows", attenuateShadows ? 0 : 1);
            resolveShadowsEffectTechnique["DrawShadows"].SetParameter("MaskProps", MaskProps);
            resolveShadowsEffectTechnique["DrawShadows"].SetParameter("DiffuseColor", diffuseColor);
            
            
            CluwneSprite Sprite = new CluwneSprite("Maskspritetorendergarget", MaskTexture);
            RenderImage MaskTarget = new RenderImage("MaskTarget", (uint)Sprite.Size.X, (uint)Sprite.Size.Y);
            ExecuteTechnique(MaskTarget, Result, "DrawShadows", shadowMap);
            ExecuteTechnique(shadowsRT, processedShadowsRT, "BlurHorizontally");
            ExecuteTechnique(processedShadowsRT, Result, "BlurVerticallyAndAttenuate");

            resolveShadowsEffectTechnique["DrawShadows"].ResetCurrentShader();
        }

        private void DebugTex(RenderImage src, RenderImage dst)
        {
            CluwneLib.ResetShader();
            dst.BeginDrawing();
            src.Blit(0, 0, dst.Width, dst.Height, BlitterSizeMode.Scale);
            dst.EndDrawing();
        }

        public void ResolveShadows(LightArea Area, bool attenuateShadows)
        {
            Texture shadowCastersTexture = Area.RenderTarget.Texture;
            RenderImage Result = Area.RenderTarget;
            Vector2 LightPosition = Area.LightPosition;
            Texture MaskTexture = Area.Mask.Texture;
            Vector4 MaskProps = Area.MaskProps;
            Vector4 diffuseColor = Vector4.One;


            //only DrawShadows needs these vars
            resolveShadowsEffectTechnique["DrawShadows"].SetParameter("AttenuateShadows", attenuateShadows ? 0 : 1);
            resolveShadowsEffectTechnique["DrawShadows"].SetParameter("MaskProps", MaskProps);
            resolveShadowsEffectTechnique["DrawShadows"].SetParameter("DiffuseColor", diffuseColor);


            ExecuteTechnique(Area.RenderTarget, distancesRT, "ComputeDistances");
            ExecuteTechnique(distancesRT, distortRT, "Distort");
            //fix
            ApplyHorizontalReduction(distortRT, shadowMap);

            CluwneSprite Sprite = new CluwneSprite("Maskspritetorendergarget", MaskTexture);
            RenderImage MaskTarget = new RenderImage("MaskTarget", (uint)Sprite.Size.X, (uint)Sprite.Size.Y);


            ExecuteTechnique(MaskTarget, Result, "DrawShadows", shadowMap);
            ExecuteTechnique(shadowsRT, processedShadowsRT, "BlurHorizontally");
            ExecuteTechnique(processedShadowsRT, Result, "BlurVerticallyAndAttenuate");

            resolveShadowsEffectTechnique["DrawShadows"].ResetCurrentShader();
        }

        private void ExecuteTechnique(RenderImage source, RenderImage destination, string techniqueName)
        {
            ExecuteTechnique(source, destination, techniqueName, null);
        }

        private void ExecuteTechnique(RenderImage source, RenderImage destinationTarget, string techniqueName, RenderImage shadowMap)
        {
            Vector2 renderTargetSize;
            renderTargetSize = new Vector2(baseSize, baseSize);

            destinationTarget.BeginDrawing();
            destinationTarget.Clear(Color.White);

            resolveShadowsEffectTechnique[techniqueName].setAsCurrentShader() ;

            resolveShadowsEffectTechnique[techniqueName].SetParameter("renderTargetSize", renderTargetSize);
            if (source != null)
                resolveShadowsEffectTechnique[techniqueName].SetParameter("InputTexture", source);
            if (shadowMap != null)
                resolveShadowsEffectTechnique[techniqueName].SetParameter("ShadowMapTexture", shadowMap);

            // Blit and use normal sampler instead of doing that weird InputTexture bullshit
            source.Blit(0, 0, source.Width, source.Height, BlitterSizeMode.Crop);

           destinationTarget.EndDrawing();
        }



        private void ApplyHorizontalReduction(RenderImage source, RenderImage destination)
        {
            int step = reductionChainCount - 1;
            RenderImage src = source;
            RenderImage HorizontalReduction= reductionRT[step];
            reductionEffectTechnique["HorizontalReduction"].setAsCurrentShader();

            GLTexture GLHorizontalReduction = new GLTexture("desto", (int)source.Width, (int)source.Height, ImageBufferFormats.BufferGR1616F);
           

            while (step >= 0)
            {
                HorizontalReduction = reductionRT[step]; // next step

                HorizontalReduction.BeginDrawing();
                HorizontalReduction.Clear(Color.White);
            
                var textureDim = new Vector2(1.0f/src.Width, 1.0f/src.Height);
                reductionEffectTechnique["HorizontalReduction"].SetParameter("TextureDimensions",textureDim);


                // Sourcetexture not needed... just blit!
                src.Blit(HorizontalReduction); // draw SRC to HR
                
                
                //fix
                GLHorizontalReduction.Blit(src.Texture, CluwneLib.CurrentShader);

                HorizontalReduction.EndDrawing();

                src = HorizontalReduction; // hr becomes new src 
                step--;
            }



            //copy to destination
            destination.BeginDrawing();
            destination.Clear(Color.White);
           
                reductionEffectTechnique["Copy"].SetParameter("SourceTexture", GLSLShader.CurrentTexture);
                reductionEffectTechnique["Copy"].setAsCurrentShader();

                GLHorizontalReduction.Blit(HorizontalReduction.Texture, CluwneLib.CurrentShader); 
            destination.EndDrawing();

            //destination.Texture.CopyToImage().SaveToFile("..\\GLTexture.png");
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