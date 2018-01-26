using SS14.Client.Graphics.Render;
using SS14.Client.Graphics.Textures;
using SS14.Client.Graphics.Utility;
using SS14.Shared.Maths;
using System;
using SSprite = SFML.Graphics.Sprite;
using STransformable = SFML.Graphics.Transformable;

namespace SS14.Client.Graphics.Sprites
{
    public class Sprite : Transformable, IDrawable, IDisposable
    {
        private Texture _texture;
        public Texture Texture
        {
            get => _texture;
            set
            {
                SFMLSprite.Texture = value?.SFMLTexture;
                _texture = value;
            }
        }
        public SSprite SFMLSprite { get; }
        public override STransformable SFMLTransformable => SFMLSprite;

        public Color Color
        {
            get => SFMLSprite.Color.Convert();
            set => SFMLSprite.Color = value.Convert();
        }

        public Box2i TextureRect
        {
            get => SFMLSprite.TextureRect.Convert();
            set => SFMLSprite.TextureRect = value.Convert();
        }

        public Box2 LocalBounds => SFMLSprite.GetLocalBounds().ToBox();

        public SFML.Graphics.Drawable SFMLDrawable => SFMLSprite;

        public Sprite(Texture texture)
        {
            SFMLSprite = new SSprite(texture.SFMLTexture);
            _texture = texture;
        }

        public Sprite(Texture texture, Box2i rect)
        {
            SFMLSprite = new SSprite(texture.SFMLTexture, rect.Convert());
            _texture = texture;
        }

        private Sprite(SSprite sprite)
        {
            SFMLSprite = sprite;
            _texture = new Texture(SFMLSprite.Texture);
        }

        public Sprite(Sprite sprite)
        {
            SFMLSprite = new SSprite(sprite.SFMLSprite);
            _texture = sprite._texture;
        }

        public void Draw(IRenderTarget target, RenderStates states)
        {
            SFMLSprite.Draw(target.SFMLTarget, states.SFMLRenderStates);
        }

        public void Draw()
        {
            Draw(CluwneLib.CurrentRenderTarget, CluwneLib.ShaderRenderState);
        }

        public void SetTransformToRect(Box2i rect)
        {
            Scale = rect.Size / (Vector2)TextureRect.Size;
            Position = rect.TopLeft;
        }
    }
}
