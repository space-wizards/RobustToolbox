using SS14.Shared.Maths;

namespace SS14.Client.Graphics
{
    public abstract class TextureSource
    {
        public abstract Godot.Texture Texture { get; }

        public int Width => Texture.GetWidth();
        public int Height => Texture.GetHeight();
        public Vector2i Size => new Vector2i(Width, Height);

        public static implicit operator Godot.Texture(TextureSource src)
        {
            return src.Texture;
        }
    }

    public class GodotTextureSource : TextureSource
    {
        public override Godot.Texture Texture { get; }

        public GodotTextureSource(Godot.Texture texture)
        {
            Texture = texture;
        }
    }
}
