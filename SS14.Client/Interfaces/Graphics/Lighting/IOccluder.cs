using System;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Shared.Maths;

namespace SS14.Client.Interfaces.Graphics.Lighting
{
    public interface IOccluder : IDisposable
    {
        bool Enabled { get; set; }

        void SetPolygon(Vector2[] polygon);
        void SetGodotPolygon(Godot.Vector2[] polygon);

        void ParentTo(IGodotTransformComponent node);
        void DeParent();
    }
}
