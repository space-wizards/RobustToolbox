using System;

namespace Robust.Client.UserInterface.Controls
{
    [Obsolete("Use SplitContainer directly and set Orientation")]
    public class HSplitContainer : SplitContainer
    {
        public HSplitContainer()
        {
            Orientation = SplitOrientation.Horizontal;
        }
    }
}
