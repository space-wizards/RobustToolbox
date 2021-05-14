namespace Robust.Shared.GameObjects
{
    internal sealed class TransformStartLerpMessage : EntityEventArgs
    {
        public TransformStartLerpMessage(TransformComponent transform)
        {
            Transform = transform;
        }

        public TransformComponent Transform { get; }
    }
}
