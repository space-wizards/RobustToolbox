using SFML.Graphics;
using SFML.System;
using SS14.Shared.Maths;

namespace SS14.Client.Graphics.Sprites
{
    class Sprite : ISprite
    {
        private SFML.Graphics.Sprite sprite;

        public ITexture Texture;
    }
}
