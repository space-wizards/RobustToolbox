using System;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     Container that lays its children out vertically: from top to bottom.
    /// </summary>
    [Obsolete("Use BoxContainer and set Orientation instead")]
    public class VBoxContainer : BoxContainer
    {
        public VBoxContainer()
        {
            Orientation = LayoutOrientation.Vertical;
        }
    }
}
