using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     A container lays out its children by some implementation-dependent rules.
    /// </summary>
    [ControlWrap("Container")]
    public abstract class Container : Control
    {
        protected Container() : base()
        {
        }

        protected Container(string name) : base(name)
        {
        }

        /// <summary>
        ///     Called when the container should re-sort its children.
        /// </summary>
        protected internal virtual void SortChildren()
        {

        }

        protected override void ChildAdded(Control newChild)
        {
            base.ChildAdded(newChild);

            newChild.OnMinimumSizeChanged += _childChanged;
            newChild.OnVisibilityChanged += _childChanged;
            MinimumSizeChanged();
            SortChildren();
        }

        protected override void ChildRemoved(Control child)
        {
            base.ChildRemoved(child);

            child.OnMinimumSizeChanged -= _childChanged;
            child.OnVisibilityChanged -= _childChanged;
            MinimumSizeChanged();
            SortChildren();
        }

        protected void FitChildInPixelBox(Control child, UIBox2i pixelBox)
        {
            var topLeft = pixelBox.TopLeft / UIScale;
            var bottomRight = pixelBox.BottomRight / UIScale;

            FitChildInBox(child, new UIBox2(topLeft, bottomRight));
        }

        protected void FitChildInBox(Control child, UIBox2 box)
        {
            DebugTools.Assert(child.Parent == this);

            var (minX, minY) = child.CombinedMinimumSize;
            var newPosX = box.Left;
            var newSizeX = minX;

            if ((child.SizeFlagsHorizontal & SizeFlags.ShrinkEnd) != 0)
            {
                newPosX += (box.Width - minX);
            }
            else if ((child.SizeFlagsHorizontal & SizeFlags.ShrinkCenter) != 0)
            {
                newPosX += (box.Width - minX) / 2;
            }
            else if ((child.SizeFlagsHorizontal & SizeFlags.Fill) != 0)
            {
                newSizeX = box.Width;
            }

            var newPosY = box.Top;
            var newSizeY = minY;

            if ((child.SizeFlagsVertical & SizeFlags.ShrinkEnd) != 0)
            {
                newPosY += (box.Height - minY);
            }
            else if ((child.SizeFlagsVertical & SizeFlags.ShrinkCenter) != 0)
            {
                newPosY += (box.Height - minY) / 2;
            }
            else if ((child.SizeFlagsVertical & SizeFlags.Fill) != 0)
            {
                newSizeY = box.Height;
            }

            child.SetAnchorPreset(LayoutPreset.TopLeft, true);

            child.Position = new Vector2(newPosX, newPosY);
            child.Size = new Vector2(newSizeX, newSizeY);
        }

        private void _childChanged(Control child)
        {
            MinimumSizeChanged();
            SortChildren();
        }

        protected override void Resized()
        {
            base.Resized();

            SortChildren();
        }
    }
}
