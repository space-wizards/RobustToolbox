using System;
using SS14.Shared.Input;

namespace SS14.Shared.GameObjects
{
    [Serializable]
    public class ClickedOnEntityMessage : EntitySystemMessage
    {
        public EntityUid Clicked { get; set; }
        public ClickType MouseButton { get; set; }
    }
}
