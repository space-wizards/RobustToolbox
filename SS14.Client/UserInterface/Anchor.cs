using System;

namespace SS14.Client.UserInterface
{
    [Flags]
    public enum Anchor
    {
        None   = 0,
        Right  = 1 << 0,
        Top    = 1 << 1,
        Left   = 1 << 2,
        Bottom = 1 << 3,
        VCenter = Top & Bottom,
        HCenter = Left & Right,
        All = Right & Top & Left & Bottom,
    }
}
