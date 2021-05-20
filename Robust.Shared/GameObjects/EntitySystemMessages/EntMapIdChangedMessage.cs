using Robust.Shared.Map;

namespace Robust.Shared.GameObjects
{
    public class EntMapIdChangedMessage : EntityEventArgs
    {
        public EntMapIdChangedMessage(IEntity entity, MapId oldMapId)
        {
            Entity = entity;
            OldMapId = oldMapId;
        }

        public IEntity Entity { get; }
        public MapId OldMapId { get; }
    }
}
