using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Physics;

namespace Robust.Shared.Physics
{
    public sealed class DebugDrawRayMessage : EntitySystemMessage
    {
        public DebugRayData Data { get; }

        public DebugDrawRayMessage(DebugRayData data)
        {
            Data = data;
        }
    }
}
