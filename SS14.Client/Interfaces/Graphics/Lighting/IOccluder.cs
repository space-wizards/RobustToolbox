using System;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Maths;

namespace SS14.Client.Interfaces.Graphics.Lighting
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
