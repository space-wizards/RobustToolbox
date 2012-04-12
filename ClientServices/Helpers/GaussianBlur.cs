using System;
using ClientInterfaces;
using ClientInterfaces.Resource;
using GorgonLibrary;
using GorgonLibrary.Graphics;

namespace ClientServices.Helpers
{
    class GaussianBlur
    {
        private readonly FXShader _shader;

        Sprite _intermediateTargetSprite;
        readonly RenderImage _intermediateTarget;

        /// <summary>
        /// Returns the radius of the Gaussian blur filter kernel in pixels.
        /// </summary>
        public int Radius { get; private set; }

        /// <summary>
        /// Returns the blur amount. This value is used to calculate the
        /// Gaussian blur filter kernel's sigma value. Good values for this
        /// property are 2 and 3. 2 will give a more blurred result whilst 3
        /// will give a less blurred result with sharper details.
        /// </summary>
        public float Amount { get; private set; }

        /// <summary>
        /// Returns the Gaussian blur filter's standard deviation.
        /// </summary>
        public float Sigma { get; private set; }

        /// <summary>
        /// Returns the Gaussian blur filter kernel matrix. Note that the
        /// kernel returned is for a 1D Gaussian blur filter kernel matrix
        /// intended to be used in a two pass Gaussian blur operation.
        /// </summary>
        public float[] Kernel { get; private set; }

        /// <summary>
        /// Returns the texture offsets used for the horizontal Gaussian blur
        /// pass.
        /// </summary>
        public Vector4D[] TextureOffsetsX { get; private set; }

        /// <summary>
        /// Returns the texture offsets used for the vertical Gaussian blur
        /// pass.
        /// </summary>
        public Vector4D[] TextureOffsetsY { get; private set; }

        private readonly IResourceManager _resourceManager;

        /// <summary>
        /// Default constructor for the GaussianBlur class. This constructor
        /// should be called if you don't want the GaussianBlur class to use
        /// its GaussianBlur.fx effect file to perform the two pass Gaussian
        /// blur operation.
        /// </summary>
        public GaussianBlur(IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;

            _intermediateTarget = new RenderImage("gaussianIntermediateTarget", Gorgon.Screen.Width, Gorgon.Screen.Height, ImageBufferFormats.BufferRGB888A8);
            _intermediateTargetSprite = new Sprite("gaussianIntermediateTargetSprite", _intermediateTarget);

            _shader = _resourceManager.GetShader("GaussianBlur");
            ComputeKernel(7, 5.0f);
            //ComputeOffsets(Gorgon.Screen.Width, Gorgon.Screen.Height);
            ComputeOffsets(256.0f, 256.0f);
        }

        public void Dispose()
        {
            if (_intermediateTarget != null && Gorgon.IsInitialized)
            {
                _intermediateTarget.ForceRelease();
                _intermediateTarget.Dispose();
            }
            if (_intermediateTargetSprite != null && Gorgon.IsInitialized)
            {
                _intermediateTargetSprite.Image = null;
                _intermediateTargetSprite = null;
            }
        }

        public void SetAmount(float amount)
        {
            ComputeKernel(7, amount);
        }

        public void SetSize(float size)
        {
            ComputeOffsets(size, size);
        }

        /// <summary>
        /// Calculates the Gaussian blur filter kernel. This implementation is
        /// ported from the original Java code appearing in chapter 16 of
        /// "Filthy Rich Clients: Developing Animated and Graphical Effects for
        /// Desktop Java".
        /// </summary>
        /// <param name="blurRadius">The blur radius in pixels.</param>
        /// <param name="blurAmount">Used to calculate sigma.</param>
        public void ComputeKernel(int blurRadius, float blurAmount)
        {
            Radius = blurRadius;
            Amount = blurAmount;

            Kernel = null;
            Kernel = new float[Radius * 2 + 1];
            Sigma = Radius / Amount;

            var twoSigmaSquare = 2.0f * Sigma * Sigma;
            var sigmaRoot = (float)Math.Sqrt(twoSigmaSquare * Math.PI);
            var total = 0.0f;

            for (var i = -Radius; i <= Radius; ++i)
            {
                float distance = i * i;
                var index = i + Radius;
                Kernel[index] = (float)Math.Exp(-distance / twoSigmaSquare) / sigmaRoot;
                total += Kernel[index];
            }

            for (var i = 0; i < Kernel.Length; ++i)
                Kernel[i] /= total;
        }

        /// <summary>
        /// Calculates the texture coordinate offsets corresponding to the
        /// calculated Gaussian blur filter kernel. Each of these offset values
        /// are added to the current pixel's texture coordinates in order to
        /// obtain the neighboring texture coordinates that are affected by the
        /// Gaussian blur filter kernel. This implementation has been adapted
        /// from chapter 17 of "Filthy Rich Clients: Developing Animated and
        /// Graphical Effects for Desktop Java".
        /// </summary>
        /// <param name="textureWidth">The texture width in pixels.</param>
        /// <param name="textureHeight">The texture height in pixels.</param>
        public void ComputeOffsets(float textureWidth, float textureHeight)
        {
            TextureOffsetsX = null;
            TextureOffsetsX = new Vector4D[Radius * 2 + 1];

            TextureOffsetsY = null;
            TextureOffsetsY = new Vector4D[Radius * 2 + 1];

            var xOffset = 1.0f / textureWidth;
            var yOffset = 1.0f / textureHeight;

            for (var i = -Radius; i <= Radius; ++i)
            {
                var index = i + Radius;
                TextureOffsetsX[index] = new Vector4D(i * xOffset, 0.0f, 0.0f, 0.0f);
                TextureOffsetsY[index] = new Vector4D(0.0f, i * yOffset, 0.0f, 0.0f);
            }
        }

        public void PerformGaussianBlur(Sprite sourceSprite, RenderImage sourceImage)
        {
            // Perform horizontal Gaussian blur.
            _intermediateTarget.Clear(System.Drawing.Color.FromArgb(0, System.Drawing.Color.Black));
            //game.GraphicsDevice.SetRenderTarget(renderTarget1);
            Gorgon.CurrentRenderTarget = _intermediateTarget;
            Gorgon.CurrentShader = _shader;

            _shader.Parameters["weights"].SetValue(Kernel);
            _shader.Parameters["colorMapTexture"].SetValue(sourceImage);
            _shader.Parameters["offsets"].SetValue(TextureOffsetsX);

            sourceSprite.Draw();

            // Perform vertical Gaussian blur.
            Gorgon.CurrentRenderTarget = sourceImage;
            
            _shader.Parameters["colorMapTexture"].SetValue(_intermediateTarget);
            _shader.Parameters["offsets"].SetValue(TextureOffsetsY);

            _intermediateTargetSprite.Draw();

            Gorgon.CurrentShader = null;
            Gorgon.CurrentRenderTarget = null;
        }

        public void PerformGaussianBlur(RenderImage sourceImage)
        {
            // Perform horizontal Gaussian blur.
            _intermediateTarget.Clear(System.Drawing.Color.FromArgb(0, System.Drawing.Color.Black));
            //game.GraphicsDevice.SetRenderTarget(renderTarget1);
            Gorgon.CurrentRenderTarget = _intermediateTarget;
            Gorgon.CurrentShader = _shader;

            _shader.Parameters["weights"].SetValue(Kernel);
            _shader.Parameters["colorMapTexture"].SetValue(sourceImage);
            _shader.Parameters["offsets"].SetValue(TextureOffsetsX);

            sourceImage.Blit(0,0,sourceImage.Width, sourceImage.Height);

            // Perform vertical Gaussian blur.
            Gorgon.CurrentRenderTarget = sourceImage;

            _shader.Parameters["colorMapTexture"].SetValue(_intermediateTarget);
            _shader.Parameters["offsets"].SetValue(TextureOffsetsY);

            _intermediateTargetSprite.Draw();

            Gorgon.CurrentShader = null;
            Gorgon.CurrentRenderTarget = null;
        }
    }
}
