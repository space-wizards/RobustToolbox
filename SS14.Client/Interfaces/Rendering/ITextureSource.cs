namespace SS14.Client.Interfaces.Rendering
{
    // Some textures are loaded by Godot's resource loader directly,
    //  for example when loaded from GUI scenes.
    // This wraps that so we can sanely handle both ResourceCache and Godot textures.
    /// <summary>
    ///     A generic source for a Godot texture.
    /// </summary>
    public interface ITextureSource
    {
        Godot.Texture Texture { get; }
    }
}
