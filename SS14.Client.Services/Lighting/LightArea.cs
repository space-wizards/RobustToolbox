using SFML.Graphics;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Render;
using SS14.Client.Interfaces.Lighting;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using System.Drawing;

namespace SS14.Client.Services.Lighting
{
    public class LightArea : ILightArea
    {

        public RenderImage RenderTarget { get; private set; }

        /// <summary>
        /// World position coordinates of the light's center
        /// </summary>
        public Vector2 LightPosition { get; set; }

        public Vector2 LightAreaSize { get; set; }
        public bool Calculated { get; set; }
        public Sprite Mask { get; set; }
        public bool MaskFlipX { get; set; }
        public bool MaskFlipY { get; set; }
        public bool Rot90 { get; set; }


        public Vector4 MaskProps
        {
            get
            {
                if (Rot90 && MaskFlipX && MaskFlipY)
                    return maskPropsVec(false, false, false);
                else if (Rot90 && MaskFlipX && !MaskFlipY)
                    return maskPropsVec(true, false, true);
                else if (Rot90 && !MaskFlipX && MaskFlipY)
                    return maskPropsVec(true, true, false);
                else if (Rot90 && !MaskFlipX && !MaskFlipY)
                    return maskPropsVec(true, false, false);
                else if (!Rot90 && MaskFlipX && MaskFlipY)
                    return maskPropsVec(false, true, true);
                else if (!Rot90 && MaskFlipX && !MaskFlipY)
                    return maskPropsVec(false, true, false);
                else if (!Rot90 && !MaskFlipX && MaskFlipY)
                    return maskPropsVec(false, false, true);
                else
                    return maskPropsVec(false, false, false);
            }
        }



        public LightArea(int size)
        {
            int baseSize = 2 << (int) size;
            LightAreaSize = new Vector2(baseSize, baseSize);
            RenderTarget = new RenderImage("LightArea"+ size, (uint)baseSize, (uint)baseSize);


            Mask = IoCManager.Resolve<IResourceManager>().GetSprite("whitemask");
        }

        public LightArea(ShadowmapSize shadowmapSize)
        {
            int baseSize = 2 << (int)shadowmapSize;
            LightAreaSize = new Vector2(baseSize, baseSize);
            RenderTarget = new RenderImage("LightArea"+ shadowmapSize,(uint)baseSize, (uint)baseSize);


            Mask = IoCManager.Resolve<IResourceManager>().GetSprite("whitemask");
         
        }

        #region ILightArea Members

       
        public Vector2 ToRelativePosition(Vector2 worldPosition)
        {
            return worldPosition - (CluwneLib.WorldToScreen(LightPosition) - LightAreaSize * 0.5f);
        }

        public void BeginDrawingShadowCasters()
        {
            RenderTarget.BeginDrawing();

            RenderTarget.Clear(new SFML.Graphics.Color(0, 0, 0, 0));
        }

        public void EndDrawingShadowCasters()
        {
            RenderTarget.EndDrawing();
        }

        public void SetMask(string mask)
        {
            Mask = IoCManager.Resolve<IResourceManager>().GetSprite(mask);
        }

        #endregion

        private Vector4 maskPropsVec(bool rot, bool flipx, bool flipy)
        {
            return new Vector4(rot ? 1 : 0, flipx ? 1 : 0, flipy ? 1 : 0, 0);
        }

           
      
    }
}