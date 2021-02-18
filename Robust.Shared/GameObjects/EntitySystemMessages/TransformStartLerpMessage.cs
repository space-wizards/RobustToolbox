namespace Robust.Shared.GameObjects
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
