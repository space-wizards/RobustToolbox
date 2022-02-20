using Robust.Shared.Map;

namespace Robust.Shared.GameObjects
{
    public sealed class EntMapIdChangedMessage : EntityEventArgs
    {
        public EntMapIdChangedMessage(EntityUid entity, MapId oldMapId)
        {
            Entity = entity;
            OldMapId = oldMapId;
        }

        public EntityUid Entity { get; }
        public MapId OldMapId { get; }
    }
}
