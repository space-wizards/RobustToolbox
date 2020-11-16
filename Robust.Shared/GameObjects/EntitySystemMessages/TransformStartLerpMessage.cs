using Robust.Shared.GameObjects.Components.Transform;

namespace Robust.Shared.GameObjects.EntitySystemMessages
{
    internal sealed class TransformStartLerpMessage : EntitySystemMessage
    {
        public TransformStartLerpMessage(TransformComponent transform)
        {
            Transform = transform;
        }

        public TransformComponent Transform { get; }
    }
}
