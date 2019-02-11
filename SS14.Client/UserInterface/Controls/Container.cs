using SS14.Shared.Maths;
using SS14.Shared.Utility;

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

        protected void FitChildInBox(Control child, UIBox2 box)
        {
            DebugTools.Assert(child.Parent == this);

            var (minX, minY) = child.CombinedMinimumSize;
            var newPosX = box.Left;
            var newSizeX = minX;

            if (child.SizeFlagsHorizontal == SizeFlags.ShrinkEnd)
            {
                newPosX += (box.Width - minX);
            }
            else if (child.SizeFlagsHorizontal == SizeFlags.ShrinkCenter)
            {
                newPosX += (box.Width - minX) / 2;
            }
            else if ((child.SizeFlagsHorizontal & SizeFlags.Fill) != 0)
            {
                newSizeX = box.Width;
            }

            var newPosY = box.Top;
            var newSizeY = minY;

            if (child.SizeFlagsVertical == SizeFlags.ShrinkEnd)
            {
                newPosX += (box.Height - minY);
            }
            else if (child.SizeFlagsVertical == SizeFlags.ShrinkCenter)
            {
                newPosX += (box.Height - minY) / 2;
            }
            else if ((child.SizeFlagsVertical & SizeFlags.Fill) != 0)
            {
                newSizeY = box.Height;
            }

            child.Position = new Vector2(newPosX, newPosY);
            child.Size = new Vector2(newSizeX, newSizeY);
        }

        private void _childMinSizeChanged(Control child)
        {
            MinimumSizeChanged();
            SortChildren();
        }
    }
}
