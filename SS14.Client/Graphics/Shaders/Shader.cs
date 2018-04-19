namespace SS14.Client.Graphics.Shaders
{
    public sealed class Shader
    {
        internal readonly Godot.Material GodotMaterial;

        internal Shader(Godot.Material godotMaterial)
        {
            GodotMaterial = godotMaterial;
        }
    }
}
