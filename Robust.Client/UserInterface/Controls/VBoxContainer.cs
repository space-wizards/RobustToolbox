namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     Container that lays its children out vertically: from top to bottom.
    /// </summary>
    [ControlWrap("VBoxContainer")]
    public class VBoxContainer : BoxContainer
    {
        public VBoxContainer()
        {
        }

        public VBoxContainer(string name) : base(name)
        {
        }

        private protected override bool Vertical => true;
    }
}
