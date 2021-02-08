using System;
using System.Collections.Generic;
using System.Text;
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
        public abstract void DrawPolygonShape(List<Vector2> vertices, in Color color);

        public abstract void SetTransform(in Matrix3 transform);
        public abstract Color CalcWakeColor(Color color, float wakePercent);
    }
}
