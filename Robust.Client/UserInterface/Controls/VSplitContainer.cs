using System;

namespace Robust.Client.UserInterface.Controls
{
    [Obsolete("Use SplitContainer directly and set Orientation")]
    public class VSplitContainer : SplitContainer
    {
        public VSplitContainer()
        {
            Orientation = SplitOrientation.Vertical;
        }
    }
}
