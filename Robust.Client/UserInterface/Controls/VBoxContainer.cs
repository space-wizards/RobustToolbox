namespace Robust.Client.UserInterface.Controls
{
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
