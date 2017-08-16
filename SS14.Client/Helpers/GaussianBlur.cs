using OpenTK;
using SFML.Graphics;
using SFML.System;
using SS14.Client.Graphics.Render;
using SS14.Client.Graphics.Shader;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.Utility;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using System;
using System.Linq;

namespace SS14.Client.Helpers
{
    public class GaussianBlur
    {
        private readonly IResourceCache _resourceCache;
        private readonly string targetName;
        private RenderImage _intermediateTarget;
        private uint lastWidth;
        private uint lastHeight;
        private TechniqueList GaussianBlurTechnique;
        private bool done = false;


        /// <summary>
        /// Default constructor for the GaussianBlur class. This constructor
        /// should be called if you don't want the GaussianBlur class to use
        /// its GaussianBlur.fx effect file to perform the two pass Gaussian
        /// blur operation.
        /// </summary>
        public GaussianBlur(IResourceCache resourceCache)
        {
            _resourceCache = resourceCache;

            targetName = "gaussTarget" + IoCManager.Resolve<IRand>().Next(0, 1000000000);

            //Set Defaults
            Radius = 7;
            Amount = 2.5f;
            Size = new Vector2(256.0f, 256.0f);

            LoadShaders();
            //ComputeOffsets(CluwneLib.Screen.Size.X, CluwneLib.Screen.Size.Y);

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
        public Vector2[] WeightsOffsetsX { get; private set; }

        /// <summary>
        /// Returns the weights and texture offsets used for the vertical Gaussian blur
        /// pass.
        /// </summary>
        public Vector2[] WeightsOffsetsY { get; private set; }

        public Vector2 Size { get; private set; }

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
            SetSize(new Vector2(size, size));
        }

        public void SetSize(Vector2 size)
        {
            Size = size;
            ComputeOffsets();
        }

        private void SetUpIntermediateTarget(uint width, uint height)
        {
            if (_intermediateTarget == null || width != lastWidth || height != lastHeight)
            {
                _intermediateTarget = new RenderImage("intermediateTarget", width, height);
                _intermediateTarget.Key = targetName;
                lastWidth = width;
                lastHeight = height;
            }
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
            done = false;
            ComputeKernel();
            LoadShaders();
        }

        private void LoadShaders()
        {
            if (!done)
            {
                GaussianBlurTechnique = _resourceCache.GetTechnique(("GaussianBlur" + Radius));
                done = true;
            }
        }

        /// <summary>
        /// Calculates the Gaussian blur filter kernel. This implementation is
        /// ported from the original Java code appearing in chapter 16 of
        /// "Filthy Rich Clients: Developing Animated and Graphical Effects for
        /// Desktop Java".
        ///
        /// Honk Java Honk
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
            float textureWidth = Size.X;
            float textureHeight = Size.Y;
            if (Kernel == null)
                ComputeKernel();

            WeightsOffsetsX = null;
            WeightsOffsetsY = null;
            WeightsOffsetsX = new Vector2[Radius*2 + 1];
            WeightsOffsetsY = new Vector2[Radius*2 + 1];

            float xOffset = 1.0f/textureWidth;
            float yOffset = 1.0f/textureHeight;

            for (int i = -Radius; i <= Radius; ++i)
            {
                int index = i + Radius;
                WeightsOffsetsX[index] = new Vector2(Kernel[index], i*xOffset);
                WeightsOffsetsY[index] = new Vector2(Kernel[index], i*yOffset);
            }
        }

        public void PerformGaussianBlur(RenderImage sourceImage)
        {

            // Blur the source horizontally
            SetUpIntermediateTarget(sourceImage.Width, sourceImage.Height);
            _intermediateTarget.BeginDrawing();
            _intermediateTarget.Clear(Color.Black);
                GaussianBlurTechnique["GaussianBlur" + Radius + "Horizontal"].SetParameter("colorMapTexture", GLSLShader.CurrentTexture);
                GaussianBlurTechnique["GaussianBlur" + Radius + "Horizontal"].SetParameter("weights_offsets", WeightsOffsetsX.Select(v => v.Convert()).ToArray());
                GaussianBlurTechnique["GaussianBlur" + Radius + "Horizontal"].setAsCurrentShader(); //.Techniques["GaussianBlurHorizontal"];
                sourceImage.Blit(0, 0, sourceImage.Width, sourceImage.Height);
            _intermediateTarget.EndDrawing();
            GaussianBlurTechnique["GaussianBlur" + Radius + "Horizontal"].ResetCurrentShader();


            //// blur the blur vertically
            sourceImage.BeginDrawing();
                GaussianBlurTechnique["GaussianBlur" + Radius + "Vertical"].SetParameter("colorMapTexture", GLSLShader.CurrentTexture);
                GaussianBlurTechnique["GaussianBlur" + Radius + "Vertical"].SetParameter("weights_offsets", WeightsOffsetsY.Select(v => v.Convert()).ToArray());
                GaussianBlurTechnique["GaussianBlur" + Radius + "Vertical"].setAsCurrentShader() ; //.Techniques["GaussianBlurVertical"];
                _intermediateTarget.Blit(0, 0, _intermediateTarget.Width, _intermediateTarget.Height);
            sourceImage.EndDrawing();
            GaussianBlurTechnique["GaussianBlur" + Radius + "Vertical"].ResetCurrentShader();
        }
    }
}
