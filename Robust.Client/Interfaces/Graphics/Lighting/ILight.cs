using System;
using Robust.Client.Graphics;
using Robust.Shared;
using Robust.Shared.Maths;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Enums;
using Robust.Shared.Interfaces.GameObjects.Components;

namespace Robust.Client.Interfaces.Graphics.Lighting
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

        void ParentTo(ITransformComponent node);
        void DeParent();
    }
}
