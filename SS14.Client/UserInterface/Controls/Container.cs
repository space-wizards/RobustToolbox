namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.Container))]
    public class Container : Control
    {
        public Container() : base()
        {
        }

        public Container(string name) : base(name)
        {
        }

        internal Container(Godot.Container sceneControl) : base(sceneControl)
        {
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.Container();
        }

        protected virtual void SortChildren()
        {

        }

        protected override void ChildAdded(Control newChild)
        {
            base.ChildAdded(newChild);

            if (!GameController.OnGodot)
            {
                newChild.OnMinimumSizeChanged += _childMinSizeChanged;
                MinimumSizeChanged();
                SortChildren();
            }
        }

        protected override void ChildRemoved(Control child)
        {
            base.ChildRemoved(child);

            if (!GameController.OnGodot)
            {
                child.OnMinimumSizeChanged -= _childMinSizeChanged;
                MinimumSizeChanged();
                SortChildren();
            }
        }

        private void _childMinSizeChanged(Control child)
        {
            MinimumSizeChanged();
            SortChildren();
        }
    }
}
