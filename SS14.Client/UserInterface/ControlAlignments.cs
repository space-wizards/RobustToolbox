using System;

namespace SS14.Client.UserInterface
{
    [Flags]
    public enum ControlAlignments
    {
        None   = 0,
        Right  = 1,
        Top    = 2,
        Left   = 4,
        Bottom = 8,

        VCenter = Top | Bottom,
        HCenter = Left | Right,
        All = HCenter | VCenter
    }
}
