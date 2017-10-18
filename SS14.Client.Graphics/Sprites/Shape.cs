using SS14.Client.Graphics.Render;
using SS14.Client.Graphics.Textures;
using SS14.Client.Graphics.Utility;
using SS14.Shared.Maths;
using STransformable = SFML.Graphics.Transformable;
using SShape = SFML.Graphics.Shape;
using SDrawable = SFML.Graphics.Drawable;

namespace SS14.Client.Graphics.Sprites
{
    public abstract class Shape : Transformable, IDrawable
    {
        public abstract SShape SFMLShape { get; }
        public override STransformable SFMLTransformable => SFMLShape;
        public SDrawable SFMLDrawable => SFMLShape;

        public void Draw(IRenderTarget target, RenderStates states)
        {
            SFMLShape.Draw(target.SFMLTarget, states.SFMLRenderStates);
        }

        public void Draw()
        {
            Draw(CluwneLib.CurrentRenderTarget, CluwneLib.ShaderRenderState);
        }

        public Box2i TextureRect
        {
            get => SFMLShape.TextureRect.Convert();
            set => SFMLShape.TextureRect = value.Convert();
        }

        private Texture _texture;
        public Texture Texture
        {
            get => _texture;
            set
            {
                SFMLShape.Texture = value?.SFMLTexture;
                _texture = value;
            }
        }

        public Color FillColor
        {
            get => SFMLShape.FillColor.Convert();
            set => SFMLShape.FillColor = value.Convert();
        }

        public Color OutlineColor
        {
            get => SFMLShape.OutlineColor.Convert();
            set => SFMLShape.OutlineColor = value.Convert();
        }

        public float OutlineThickness
        {
            get => SFMLShape.OutlineThickness;
            set => SFMLShape.OutlineThickness = value;
        }

        public Vector2 GetPoint(uint index)
        {
            return SFMLShape.GetPoint(index).Convert();
        }
        public uint PointCount => SFMLShape.GetPointCount();
    }
}
