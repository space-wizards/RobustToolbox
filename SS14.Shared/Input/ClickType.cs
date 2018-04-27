using System;

namespace SS14.Shared.Input
{
    [Flags]
    public enum ClickType
    {
        None = 0,
        Left = 1 << 0,
        Right = 1 << 1,
        Middle = 1 << 2,
        Alt = 1 << 3,
        Shift = 1 << 4,
        Cntrl = 1 << 5,
        System = 1 << 6,
    }
}
