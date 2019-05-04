namespace Robust.Client.UserInterface.Controls
{
    [ControlWrap("VSplitContainer")]
    public class VSplitContainer : SplitContainer
    {
        private protected sealed override bool Vertical => true;

        public VSplitContainer() {}
    }
}
