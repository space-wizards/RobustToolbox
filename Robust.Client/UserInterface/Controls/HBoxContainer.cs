namespace Robust.Client.UserInterface.Controls
{
    [ControlWrap("HBoxContainer")]
    public class HBoxContainer : BoxContainer
    {
        public HBoxContainer() : base()
        {
        }

        public HBoxContainer(string name) : base(name)
        {
        }

        private protected override bool Vertical => false;
    }
}
