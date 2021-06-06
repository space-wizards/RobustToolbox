#if DEBUG
using System;
using System.Collections.Generic;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects
{
    [Serializable, NetSerializable]
    public sealed class RequestGridTileLookupMessage : EntityEventArgs
    {
        public GridId GridId;
        public Vector2i Indices;

        public RequestGridTileLookupMessage(GridId gridId, Vector2i indices)
        {
            GridId = gridId;
            Indices = indices;
        }
    }

    [Serializable, NetSerializable]
    public sealed class SendGridTileLookupMessage : EntityEventArgs
    {
        public GridId GridId;
        public Vector2i Indices;

        public List<EntityUid> Entities { get; }

        public SendGridTileLookupMessage(GridId gridId, Vector2i indices, List<EntityUid> entities)
        {
            Entities = entities;
        }
    }
}
#endif
