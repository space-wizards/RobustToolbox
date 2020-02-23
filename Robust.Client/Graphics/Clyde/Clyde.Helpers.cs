using OpenTK.Graphics.OpenGL4;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics.Clyde
{
    internal sealed partial class Clyde
    {
        private void GLClearColor(Color color)
        {
            GL.ClearColor(color.R, color.G, color.B, color.A);
        }

        private void SetTexture(TextureUnit unit, Texture texture)
        {
            var ct = (ClydeTexture) texture;
            SetTexture(unit, ct.TextureId);
        }

        private void SetTexture(TextureUnit unit, ClydeHandle textureId)
        {
            var glHandle = _loadedTextures[textureId].OpenGLObject;
            GL.ActiveTexture(unit);
            GL.BindTexture(TextureTarget.Texture2D, glHandle.Handle);
        }
    }
}
