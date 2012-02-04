using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using ClientResourceManager;

namespace SS13.Effects
{
    class GaussianBlur
    {
        private FXShader shader;

        Sprite intermediateTargetSprite;
        RenderImage intermediateTarget;

        private int radius;
        private float amount;
        private float sigma;
        private float[] kernel;
        private Vector4D[] offsetsHoriz;
        private Vector4D[] offsetsVert;

        /// <summary>
        /// Returns the radius of the Gaussian blur filter kernel in pixels.
        /// </summary>
        public int Radius
        {
            get { return radius; }
        }

        /// <summary>
        /// Returns the blur amount. This value is used to calculate the
        /// Gaussian blur filter kernel's sigma value. Good values for this
        /// property are 2 and 3. 2 will give a more blurred result whilst 3
        /// will give a less blurred result with sharper details.
        /// </summary>
        public float Amount
        {
            get { return amount; }
        }

        /// <summary>
        /// Returns the Gaussian blur filter's standard deviation.
        /// </summary>
        public float Sigma
        {
            get { return sigma; }
        }

        /// <summary>
        /// Returns the Gaussian blur filter kernel matrix. Note that the
        /// kernel returned is for a 1D Gaussian blur filter kernel matrix
        /// intended to be used in a two pass Gaussian blur operation.
        /// </summary>
        public float[] Kernel
        {
            get { return kernel; }
        }

        /// <summary>
        /// Returns the texture offsets used for the horizontal Gaussian blur
        /// pass.
        /// </summary>
        public Vector4D[] TextureOffsetsX
        {
            get { return offsetsHoriz; }
        }

        /// <summary>
        /// Returns the texture offsets used for the vertical Gaussian blur
        /// pass.
        /// </summary>
        public Vector4D[] TextureOffsetsY
        {
            get { return offsetsVert; }
        }

        /// <summary>
        /// Default constructor for the GaussianBlur class. This constructor
        /// should be called if you don't want the GaussianBlur class to use
        /// its GaussianBlur.fx effect file to perform the two pass Gaussian
        /// blur operation.
        /// </summary>
        public GaussianBlur()
        {
            intermediateTarget = new RenderImage("gaussianIntermediateTarget", Gorgon.Screen.Width, Gorgon.Screen.Height, ImageBufferFormats.BufferRGB888A8);
            intermediateTargetSprite = new Sprite("gaussianIntermediateTargetSprite", intermediateTarget);

            shader = ResMgr.Singleton.GetShader("GaussianBlur");
            ComputeKernel(7, 5.0f);
            //ComputeOffsets(Gorgon.Screen.Width, Gorgon.Screen.Height);
            ComputeOffsets(256.0f, 256.0f);
        }

        public void Dispose()
        {
            if (intermediateTarget != null && Gorgon.IsInitialized)
            {
                intermediateTarget.ForceRelease();
                intermediateTarget.Dispose();
            }
            if (intermediateTargetSprite != null && Gorgon.IsInitialized)
            {
                intermediateTargetSprite.Image = null;
                intermediateTargetSprite = null;
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
            radius = blurRadius;
            amount = blurAmount;

            kernel = null;
            kernel = new float[radius * 2 + 1];
            sigma = radius / amount;

            float twoSigmaSquare = 2.0f * sigma * sigma;
            float sigmaRoot = (float)Math.Sqrt(twoSigmaSquare * Math.PI);
            float total = 0.0f;
            float distance = 0.0f;
            int index = 0;

            for (int i = -radius; i <= radius; ++i)
            {
                distance = i * i;
                index = i + radius;
                kernel[index] = (float)Math.Exp(-distance / twoSigmaSquare) / sigmaRoot;
                total += kernel[index];
            }

            for (int i = 0; i < kernel.Length; ++i)
                kernel[i] /= total;
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
            offsetsHoriz = null;
            offsetsHoriz = new Vector4D[radius * 2 + 1];

            offsetsVert = null;
            offsetsVert = new Vector4D[radius * 2 + 1];

            int index = 0;
            float xOffset = 1.0f / textureWidth;
            float yOffset = 1.0f / textureHeight;

            for (int i = -radius; i <= radius; ++i)
            {
                index = i + radius;
                offsetsHoriz[index] = new Vector4D(i * xOffset, 0.0f, 0.0f, 0.0f);
                offsetsVert[index] = new Vector4D(0.0f, i * yOffset, 0.0f, 0.0f);
            }
        }

        /// <summary>
        /// Performs the Gaussian blur operation on the source texture image.
        /// The Gaussian blur is performed in two passes: a horizontal blur
        /// pass followed by a vertical blur pass. The output from the first
        /// pass is rendered to renderTarget1. The output from the second pass
        /// is rendered to renderTarget2. The dimensions of the blurred texture
        /// is therefore equal to the dimensions of renderTarget2.
        /// </summary>
        /// <param name="srcTexture">The source image to blur.</param>
        /// <param name="renderTarget1">Stores the output from the horizontal blur pass.</param>
        /// <param name="renderTarget2">Stores the output from the vertical blur pass.</param>
        /// <param name="spriteBatch">Used to draw quads for the blur passes.</param>
        /// <returns>The resulting Gaussian blurred image.</returns>
        public void PerformGaussianBlur(Sprite SourceSprite, RenderImage SourceImage)
        {
            // Perform horizontal Gaussian blur.
            intermediateTarget.Clear(System.Drawing.Color.FromArgb(0, System.Drawing.Color.Black));
            //game.GraphicsDevice.SetRenderTarget(renderTarget1);
            Gorgon.CurrentRenderTarget = intermediateTarget;
            Gorgon.CurrentShader = shader;

            shader.Parameters["weights"].SetValue(kernel);
            shader.Parameters["colorMapTexture"].SetValue(SourceImage);
            shader.Parameters["offsets"].SetValue(offsetsHoriz);

            SourceSprite.Draw();

            // Perform vertical Gaussian blur.
            Gorgon.CurrentRenderTarget = SourceImage;
            
            shader.Parameters["colorMapTexture"].SetValue(intermediateTarget);
            shader.Parameters["offsets"].SetValue(offsetsVert);

            intermediateTargetSprite.Draw();

            Gorgon.CurrentShader = null;
            Gorgon.CurrentRenderTarget = null;

            // Return the Gaussian blurred texture.

        }
    }
}
