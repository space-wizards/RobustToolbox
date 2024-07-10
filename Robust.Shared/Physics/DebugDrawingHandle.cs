using System.Numerics;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    public abstract class DebugDrawingHandle
    {
        public abstract Color GridFillColor { get; }
        public abstract Color RectFillColor { get; }
        public abstract Color WakeMixColor { get; }

        public abstract void DrawRect(in Box2 box, in Color color);
        public abstract void DrawRect(in Box2Rotated box, in Color color);
        public abstract void DrawCircle(Vector2 origin, float radius, in Color color);
        public abstract void DrawPolygonShape(Vector2[] vertices, in Color color);
        public abstract void DrawLine(Vector2 start, Vector2 end, in Color color);

        public abstract void SetTransform(in Matrix3x2 transform);
        public abstract Color CalcWakeColor(Color color, float wakePercent);
    }
}
