using System;
using System.Numerics;
using Robust.Shared.Graphics;
using Robust.Shared.Maths;
using SixLabors.ImageSharp.PixelFormats;

namespace Robust.Client.Graphics
{
    /// <summary>
    ///     Represents something that can be rendered to.
    /// </summary>
    public interface IRenderTarget : IDisposable
    {
        /// <summary>
        ///     The size of the render target, in physical pixels.
        /// </summary>
        Vector2i Size { get; }

        void CopyPixelsToMemory<T>(CopyPixelsDelegate<T> callback, UIBox2i? subRegion = null) where T : unmanaged, IPixel<T>;

        public Vector2 LocalToWorld(IEye eye, Vector2 point, Vector2 scale)
        {
            var newPoint = point;

            // (inlined version of UiProjMatrix^-1)
            newPoint -= Size / 2f;
            newPoint *= new Vector2(1, -1) / EyeManager.PixelsPerMeter;

            // view matrix
            eye.GetViewMatrixInv(out var viewMatrixInv, scale);
            newPoint = Vector2.Transform(newPoint, viewMatrixInv);

            return newPoint;
        }

        public Vector2 WorldToLocal(Vector2 point, IEye eye, Vector2 scale)
        {
            var newPoint = point;

            eye.GetViewMatrix(out var viewMatrix, scale);
            newPoint = Vector2.Transform(newPoint, viewMatrix);

            // (inlined version of UiProjMatrix)
            newPoint *= new Vector2(1, -1) * EyeManager.PixelsPerMeter;
            newPoint += Size / 2f;

            return newPoint;
        }

        public Matrix3x2 GetWorldToLocalMatrix(IEye eye, Vector2 scale)
        {
            eye.GetViewMatrix(out var viewMatrix, scale * new Vector2(EyeManager.PixelsPerMeter, -EyeManager.PixelsPerMeter));
            viewMatrix.M31 += Size.X / 2f;
            viewMatrix.M32 += Size.Y / 2f;
            return viewMatrix;
        }
    }
}
