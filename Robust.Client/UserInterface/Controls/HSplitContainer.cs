namespace Robust.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.HSplitContainer))]
    public class HSplitContainer : SplitContainer
    {
        private protected sealed override bool Vertical => false;

        public HSplitContainer() {}
        public HSplitContainer(Godot.HSplitContainer control) : base(control) {}
    }
}
