namespace SS14.Client.Graphics.Shaders
{
    public sealed class Shader
    {
        #if GODOT
        internal readonly Godot.Material GodotMaterial;

        internal Shader(Godot.Material godotMaterial)
        {
            GodotMaterial = godotMaterial;
        }

        internal void ApplyToCanvasItem(Godot.RID item)
        {
            Godot.VisualServer.CanvasItemSetMaterial(item, GodotMaterial.GetRid());
        }
        #endif
    }
}
