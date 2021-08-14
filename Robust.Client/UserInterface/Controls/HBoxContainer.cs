using System;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     Container that lays its children out horizontally: from left to right.
    /// </summary>
    [Obsolete("Use BoxContainer and set Orientation instead")]
    public class HBoxContainer : BoxContainer
    {
        public HBoxContainer()
        {
            Orientation = LayoutOrientation.Horizontal;
        }
    }
}
