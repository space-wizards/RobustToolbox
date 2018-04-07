using SS14.Shared.Maths;

namespace SS14.Client.Graphics
{
    /// <summary>
    ///     Contains a texture used for drawing things.
    /// </summary>
    public abstract class Texture
    {
        internal abstract Godot.Texture GodotTexture { get; }

        public int Width => GodotTexture.GetWidth();
        public int Height => GodotTexture.GetHeight();
        public Vector2i Size => new Vector2i(Width, Height);

        public static implicit operator Godot.Texture(Texture src)
        {
            return src?.GodotTexture;
        }
    }

    /// <summary>
    ///     Wraps a texture returned by Godot itself,
    ///     for example when the texture was set in a GUI scene.
    /// </summary>
    internal class GodotTextureSource : Texture
    {
        internal override Godot.Texture GodotTexture { get; }

        public GodotTextureSource(Godot.Texture texture)
        {
            GodotTexture = texture;
        }
    }
}
