using GorgonLibrary;
using GorgonLibrary.Graphics;
using SS14.Client.Interfaces.Lighting;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.Utility;
using SS14.Shared.IoC;
using System.Drawing;

namespace SS14.Client.Services.Lighting
{
    public class LightArea : ILightArea
    {
        public LightArea(ShadowmapSize size)
        {
            int baseSize = 2 << (int) size;
            LightAreaSize = new Vector2D(baseSize, baseSize);
            renderTarget = new RenderImage("lightTest" + baseSize + IoCManager.Resolve<IRand>().Next(100000, 999999),
                                           baseSize, baseSize, ImageBufferFormats.BufferRGB888A8);
            Mask = IoCManager.Resolve<IResourceManager>().GetSprite("whitemask");
        }

        #region ILightArea Members

        public RenderImage renderTarget { get; private set; }

        /// <summary>
        /// World position coordinates of the light's center
        /// </summary>
        public Vector2D LightPosition { get; set; }

        public Vector2D LightAreaSize { get; set; }
        public bool Calculated { get; set; }
        public Sprite Mask { get; set; }
        public bool MaskFlipX { get; set; }
        public bool MaskFlipY { get; set; }
        public bool Rot90 { get; set; }

        public Vector4D MaskProps
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

        public Vector2D ToRelativePosition(Vector2D worldPosition)
        {
            return worldPosition - (LightPosition - LightAreaSize*0.5f);
        }

        public void BeginDrawingShadowCasters()
        {
            Gorgon.CurrentRenderTarget = renderTarget;
            Gorgon.CurrentRenderTarget.Clear(Color.FromArgb(0, 0, 0, 0));
        }

        public void EndDrawingShadowCasters()
        {
            Gorgon.CurrentRenderTarget = null;
        }

        public void SetMask(string mask)
        {
            Mask = IoCManager.Resolve<IResourceManager>().GetSprite(mask);
        }

        #endregion

        private Vector4D maskPropsVec(bool rot, bool flipx, bool flipy)
        {
            return new Vector4D(rot ? 1 : 0, flipx ? 1 : 0, flipy ? 1 : 0, 0);
        }
    }
}