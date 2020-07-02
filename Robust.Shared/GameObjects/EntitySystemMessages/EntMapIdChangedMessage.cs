using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;

namespace Robust.Shared.GameObjects.EntitySystemMessages
{
    public readonly struct EntMapIdChangedMessage
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
