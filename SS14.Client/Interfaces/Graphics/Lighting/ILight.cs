using System;
using SS14.Client.Graphics;
using SS14.Shared;
using SS14.Shared.Maths;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Shared.Enums;

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
        Texture Texture { get; set; }
        bool Enabled { get; set; }

        void ParentTo(IGodotTransformComponent node);
        void DeParent();
    }
}
