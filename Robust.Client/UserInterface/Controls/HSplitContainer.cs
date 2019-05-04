namespace Robust.Client.UserInterface.Controls
{
    [ControlWrap("HSplitContainer")]
    public class HSplitContainer : SplitContainer
    {
        private protected sealed override bool Vertical => false;

        public HSplitContainer() {}
    }
}
