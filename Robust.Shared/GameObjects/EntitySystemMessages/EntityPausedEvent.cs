namespace Robust.Shared.GameObjects
{
    public sealed class EntityPausedEvent : EntityEventArgs
    {
        public EntityUid Entity { get; }
        public bool Paused { get; }

        public EntityPausedEvent(EntityUid entity, bool paused)
        {
            Entity = entity;
            Paused = paused;
        }
    }
}
