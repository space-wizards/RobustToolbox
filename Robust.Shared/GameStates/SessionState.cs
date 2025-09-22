using Robust.Shared.Serialization;
using System;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.ViewVariables;

#nullable disable

namespace Robust.Shared.GameStates
{
    [Serializable, NetSerializable]
    public sealed class SessionState
    {
        [ViewVariables]
        public NetUserId UserId { get; set; }

        [ViewVariables]
        public string Name { get; set; }

        [ViewVariables]
        public SessionStatus Status { get; set; }

        // TODO PlayerManager
        // Network ping information, though probably do it outside of SessionState to avoid re-sending the name and such
        // for all players every few seconds.
        [Obsolete("Ping data is not currently networked")]
        [ViewVariables]
        public short Ping { get; set; }

        [ViewVariables]
        public NetEntity? ControlledEntity { get; set; }

        public SessionState Clone()
        {
            return new SessionState
            {
                UserId = UserId,
                Name = Name,
                Status = Status,
                ControlledEntity = ControlledEntity
            };
        }
    }
}
