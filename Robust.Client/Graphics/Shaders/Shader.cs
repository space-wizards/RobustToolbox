using System.Collections.Generic;

namespace Robust.Client.Graphics.Shaders
{
    public sealed class Shader
    {
        internal readonly Godot.Material GodotMaterial;
        internal readonly int ClydeHandle = -1;

        // We intentionally leak shaders to work around Godot issue #24108
        // ReSharper disable once CollectionNeverQueried.Local
        private static readonly List<Shader> LeakyLeaky = new List<Shader>();

        internal Shader()
        {

        }

        internal Shader(Godot.Material godotMaterial)
        {
            LeakyLeaky.Add(this);
            GodotMaterial = godotMaterial;
        }

        internal Shader(int clydeHandle)
        {
            ClydeHandle = clydeHandle;
        }

        internal void ApplyToCanvasItem(Godot.RID item)
        {
            Godot.VisualServer.CanvasItemSetMaterial(item, GodotMaterial.GetRid());
        }
    }
}
