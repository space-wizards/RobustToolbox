using System;

namespace Robust.Client.Debugging
{
    public interface IDebugDrawingManager
    {
        bool DebugDrawRays { get; set; }
        TimeSpan DebugRayLifetime { get; set; }
        void Initialize();
    }
}
