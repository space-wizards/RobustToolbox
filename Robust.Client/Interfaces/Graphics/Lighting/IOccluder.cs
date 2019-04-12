using System;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Maths;

namespace Robust.Client.Interfaces.Graphics.Lighting
{
    public interface IOccluder : IDisposable
    {
        bool Enabled { get; set; }

        OccluderCullMode CullMode { get; set; }

        void SetPolygon(Vector2[] polygon);

        void ParentTo(ITransformComponent node);
        void DeParent();
    }

    public enum OccluderCullMode
    {
        // These match Godot's OccluderPolygon2D.CullMode
        Disabled = 0,
        Clockwise = 1,
        CounterClockwise = 2
    }
}
