using System;
using SS14.Shared.Input;
using SS14.Shared.Serialization;

namespace SS14.Shared.GameObjects
{
    [Serializable, NetSerializable]
    public class ClickedOnEntityMessage : EntitySystemMessage
    {
        public EntityUid Clicked { get; set; }
        public ClickType MouseButton { get; set; }
    }
}
