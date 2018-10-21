using System;
using SS14.Shared.Maths;

namespace SS14.Client.Graphics
{
    /// <summary>
    ///     Contains a texture used for drawing things.
    /// </summary>
    public abstract class Texture : IDirectionalTextureProvider
    {
        internal abstract Godot.Texture GodotTexture { get; }

        public int Width => GameController.OnGodot ? GodotTexture.GetWidth() : default;
        public int Height => GameController.OnGodot ? GodotTexture.GetHeight() : default;

        public Vector2i Size => new Vector2i(Width, Height);

        public static implicit operator Godot.Texture(Texture src)
        {
            return src?.GodotTexture;
        }

        Texture IDirectionalTextureProvider.Default => this;

        Texture IDirectionalTextureProvider.TextureFor(Direction dir)
        {
            return this;
        }
    }

    /// <summary>
    ///     Blank dummy texture.
    /// </summary>
    public class BlankTexture : Texture
    {
        internal override Godot.Texture GodotTexture => null;
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
