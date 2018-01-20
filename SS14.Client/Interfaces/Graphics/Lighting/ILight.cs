using System;
using SS14.Client.Graphics;
using SS14.Shared;
using SS14.Shared.Maths;
using SS14.Client.Interfaces.GameObjects.Components;

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

        void ParentTo(IClientTransformComponent node);
        void DeParent();
    }
}
