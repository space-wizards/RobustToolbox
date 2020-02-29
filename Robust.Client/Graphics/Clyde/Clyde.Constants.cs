using Robust.Shared.Maths;

namespace Robust.Client.Graphics.Clyde
{
    internal sealed partial class Clyde
    {
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

        private static readonly Color AmbientLightColor = Color.Black;
    }
}
