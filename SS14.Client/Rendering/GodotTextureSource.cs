using SS14.Client.Interfaces.Rendering;

namespace SS14.Client.Rendering
{
    public class GodotTextureSource : ITextureSource
    {
        public Godot.Texture Texture { get; }

        public GodotTextureSource(Godot.Texture texture)
        {
            Texture = texture;
        }
    }
}
