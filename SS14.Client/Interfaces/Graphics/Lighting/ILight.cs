using System;
using SS14.Client.Graphics;
using SS14.Shared;
using SS14.Shared.Maths;

namespace SS14.Client.Interfaces.Graphics.Lighting
{
    public interface ILight : IDisposable
    {
        Vector2 Offset { get; set; }
        Angle Rotation { get; set; }
        Color Color { get; set; }
        float TextureScale { get; set; }
        float Energy { get; set; }
        ILightMode Mode { get; }
        LightModeClass ModeClass { get; set; }
        TextureSource Texture { get; set; }
        bool Enabled { get; set; }

        void ParentTo(Godot.Node node);
        void DeParent();
        void UpdateEnabled();
    }
}
