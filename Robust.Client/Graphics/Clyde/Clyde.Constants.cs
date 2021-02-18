namespace Robust.Client.Graphics.Clyde
{
    internal sealed partial class Clyde
    {
        private static readonly (string, uint)[] BaseShaderAttribLocations = {
            ("aPos", 0),
            ("tCoord", 1)
        };

        private const int UniIModUV = 0;
        private const int UniIModelMatrix = 1;
        private const int UniIModulate = 2;
        private const int UniITexturePixelSize = 3;
        private const int UniIMainTexture = 4;
        private const int UniILightTexture = 5;
        private const int UniCount = 6;

        private const string UniModUV = "modifyUV";
        private const string UniModelMatrix = "modelMatrix";
        private const string UniModulate = "modulate";
        private const string UniTexturePixelSize = "TEXTURE_PIXEL_SIZE";
        private const string UniMainTexture = "TEXTURE";
        private const string UniLightTexture = "lightMap";
        private const string UniProjViewMatrices = "projectionViewMatrices";
        private const string UniUniformConstants = "uniformConstants";

        private const int BindingIndexProjView = 0;
        private const int BindingIndexUniformConstants = 1;

        // To be clear: You shouldn't change this. This just helps with understanding where Primitive Restart is being used.
        private const ushort PrimitiveRestartIndex = ushort.MaxValue;

        private enum Renderer : short
        {
            // Default: Try all supported renderers (not necessarily the renderers shown here)
            Default = default,
            OpenGL33 = 1,
            OpenGL31 = 2,
            OpenGLES2 = 3,
            Explode = -1,
        }
    }
}
