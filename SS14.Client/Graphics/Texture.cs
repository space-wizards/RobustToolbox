using System;
using SS14.Shared.Maths;

namespace SS14.Client.Graphics
{
    /// <summary>
    ///     Contains a texture used for drawing things.
    /// </summary>
    public abstract class Texture : IDirectionalTextureProvider
    {
        #if GODOT
        internal abstract Godot.Texture GodotTexture { get; }
        #endif

        #if GODOT
        public int Width => GodotTexture.GetWidth();
        public int Height => GodotTexture.GetHeight();
        #else
        public int Width => 0;
        public int Height => 0;
        #endif
        public Vector2i Size => new Vector2i(Width, Height);

        #if GODOT
        public static implicit operator Godot.Texture(Texture src)
        {
            return src?.GodotTexture;
        }
        #endif

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

    }

    #if GODOT
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
    #endif
}
