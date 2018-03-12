using System;
using SS14.Shared.Input;
using SS14.Shared.Map;

namespace SS14.Shared.GameObjects
{
    [Serializable]
    public class BoundKeyChangedMessage : EntitySystemMessage
    {
        public BoundKeyFunctions Function { get; }
        public BoundKeyState State { get; }

        public BoundKeyChangedMessage(BoundKeyFunctions function, BoundKeyState state)
        {
            Function = function;
            State = state;
        }
    }

    [Serializable]
    public class ClickEventMessage : EntitySystemMessage
    {
        public EntityUid Uid { get; }
        public ClickType Click { get; }
        public LocalCoordinates Coordinates { get; }

        public ClickEventMessage(EntityUid uid, ClickType click, LocalCoordinates coordinates)
        {
            Uid = uid;
            Click = click;
            Coordinates = coordinates;
        }
    }
}
