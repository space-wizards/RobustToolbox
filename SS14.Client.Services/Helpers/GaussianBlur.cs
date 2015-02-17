using SS14.Client.Graphics.CluwneLib.Shader;
using SS14.Client.Graphics.CluwneLib.Render;
using SS14.Shared.Maths;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.Utility;
using SS14.Shared.IoC;
using System;
using System.Drawing;

namespace SS14.Client.Services.Helpers
{
    internal class GaussianBlur
    {
        private readonly IResourceManager _resourceManager;
        private readonly string targetName;
        private RenderImage _intermediateTarget;
        private FXShader _shader;

        /// <summary>
        /// Default constructor for the GaussianBlur class. This constructor
        /// should be called if you don't want the GaussianBlur class to use
        /// its GaussianBlur.fx effect file to perform the two pass Gaussian
        /// blur operation.
        /// </summary>
        public GaussianBlur(IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;

            targetName = "gaussTarget" + IoCManager.Resolve<IRand>().Next(0, 1000000000);

            //Set Defaults
            Radius = 7;
            Amount = 2.5f;
            Size = new SizeF(256.0f, 256.0f);
            ComputeKernel();
            SetShader();

            //ComputeOffsets(Gorgon.Screen.Width, Gorgon.Screen.Height);
            ComputeOffsets();
        }

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
        /// Returns the weights and texture offsets used for the horizontal Gaussian blur
        /// pass.
        /// </summary>
        public Vector4[] WeightsOffsetsX { get; private set; }

        /// <summary>
        /// Returns the weights and texture offsets used for the vertical Gaussian blur
        /// pass.
        /// </summary>
        public Vector4[] WeightsOffsetsY { get; private set; }

        public SizeF Size { get; private set; }

        public void Dispose()
        {
        }

        public void SetAmount(float amount)
        {
            Amount = amount;
            ComputeKernel();
            ComputeOffsets();
        }

        public void SetSize(float size)
        {
            Size = new SizeF(size, size);
            ComputeOffsets();
        }

        public void SetSize(SizeF size)
        {
            Size = size;
            ComputeOffsets();
        }

        public void SetRadius(int radius)
        {
            switch (radius)
            {
                case 3:
                case 5:
                case 7:
                case 9:
                case 11:
                    Radius = radius;
                    break;
                default:
                    throw new Exception("The blur radius must be 3, 5, 7, 9, or 11.");
            }
            ComputeKernel();
            SetShader();
        }

        private void SetShader()
        {
            _shader = _resourceManager.GetShader("GaussianBlur" + Radius);
        }

        /// <summary>
        /// Calculates the Gaussian blur filter kernel. This implementation is
        /// ported from the original Java code appearing in chapter 16 of
        /// "Filthy Rich Clients: Developing Animated and Graphical Effects for
        /// Desktop Java".
        /// </summary>
        public void ComputeKernel()
        {
            Kernel = null;
            Kernel = new float[Radius*2 + 1];
            Sigma = Radius/Amount;

            float twoSigmaSquare = 2.0f*Sigma*Sigma;
            var sigmaRoot = (float) Math.Sqrt(twoSigmaSquare*Math.PI);
            float total = 0.0f;

            for (int i = -Radius; i <= Radius; ++i)
            {
                float distance = i*i;
                int index = i + Radius;
                Kernel[index] = (float) Math.Exp(-distance/twoSigmaSquare)/sigmaRoot;
                total += Kernel[index];
            }

            for (int i = 0; i < Kernel.Length; ++i)
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
        public void ComputeOffsets()
        {
            float textureWidth = Size.Width;
            float textureHeight = Size.Height;
            if (Kernel == null)
                ComputeKernel();

            WeightsOffsetsX = null;
            WeightsOffsetsY = null;
            WeightsOffsetsX = new Vector4[Radius*2 + 1];
            WeightsOffsetsY = new Vector4[Radius*2 + 1];

            float xOffset = 1.0f/textureWidth;
            float yOffset = 1.0f/textureHeight;

            for (int i = -Radius; i <= Radius; ++i)
            {
                int index = i + Radius;
                WeightsOffsetsX[index] = new Vector4(Kernel[index], i*xOffset, 0.0f, 0.0f);
                WeightsOffsetsY[index] = new Vector4(Kernel[index], i*yOffset, 0.0f, 0.0f);
            }
        }

        public void PerformGaussianBlur(RenderImage sourceImage)
        {
            // Perform horizontal Gaussian blur.
            _intermediateTarget = new RenderImage(targetName, sourceImage.Width, sourceImage.Height, sourceImage.Format);
            _intermediateTarget.Clear(Color.Black);

            Gorgon.CurrentRenderTarget = _intermediateTarget;
            Gorgon.CurrentShader = _shader.Techniques["GaussianBlurHorizontal"];

            _shader.Parameters["weights_offsets"].SetValue(WeightsOffsetsX);
            _shader.Parameters["colorMapTexture"].SetValue(sourceImage.Image);

            sourceImage.Image.Blit(0, 0, sourceImage.Image.Width, sourceImage.Image.Height);

            // Perform vertical Gaussian blur.
            Gorgon.CurrentRenderTarget = sourceImage;
            Gorgon.CurrentShader = _shader.Techniques["GaussianBlurVertical"];

            _shader.Parameters["colorMapTexture"].SetValue(_intermediateTarget.Image);
            _shader.Parameters["weights_offsets"].SetValue(WeightsOffsetsY);

            //_intermediateTargetSprite.Draw();
            _intermediateTarget.Image.Blit(0, 0, sourceImage.Image.Width, sourceImage.Image.Height);

            Gorgon.CurrentShader = null;
            Gorgon.CurrentRenderTarget = null;
            _intermediateTarget.Dispose();
        }
    }
}