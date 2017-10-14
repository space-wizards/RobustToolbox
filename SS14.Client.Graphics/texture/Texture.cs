using SS14.Shared.Maths;
using SS14.Client.Graphics.Utility;

namespace SS14.Client.Graphics.Textures
{
    class Texture : ITexture
    {
        public Vector2u Size => texture.Size.Convert();
        private SFML.Graphics.Texture texture;

        public Texture(SFML.Graphics.Texture texture)
        {
            this.texture = texture;
        }
    }
}
