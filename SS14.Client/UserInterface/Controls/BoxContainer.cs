using System;
using System.Collections.Generic;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(BoxContainer))]
    public abstract class BoxContainer : Container
    {
        public const string StylePropertySeparation = "separation";

        private const int DefaultSeparation = 1;
        private protected abstract bool Vertical { get; }

        protected BoxContainer()
        {
        }

        protected BoxContainer(string name) : base(name)
        {
        }

        internal BoxContainer(Godot.BoxContainer sceneControl) : base(sceneControl)
        {
        }

        private AlignMode _align;

        public AlignMode Align
        {
            get => GameController.OnGodot ? (AlignMode) SceneControl.Get("align_mode") : _align;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("align_mode", (int) value);
                }
                else
                {
                    _align = value;
                }
            }
        }

        private int ActualSeparation
        {
            get
            {
                if (TryGetStyleProperty(StylePropertySeparation, out int separation))
                {
                    return separation;
                }

                return _separationOverride ?? 1;
            }
        }

        private int? _separationOverride;

        public int? SeparationOverride
        {
            get => _separationOverride ?? GetConstantOverride("separation");
            set => SetConstantOverride("separation", _separationOverride = value);
        }

        protected override void SortChildren()
        {
            var separation = ActualSeparation;

            // Step one: figure out the sizes of all our children and whether they want to stretch.
            var sizeList = new List<(Control control, int minSize, int finalSize, bool stretch)>();
            var totalStretchRatio = 0f;
            // Amount of space not available for stretching.
            var stretchMin = 0;

            foreach (var child in Children)
            {
                if (!child.Visible)
                {
                    continue;
                }
                var childMinSize = child.CombinedMinimumSize;
                int minSize;
                bool stretch;

                if (Vertical)
                {
                    minSize = (int) childMinSize.Y;
                    stretch = (child.SizeFlagsVertical & SizeFlags.Expand) == SizeFlags.Expand;
                }
                else
                {
                    minSize = (int) childMinSize.X;
                    stretch = (child.SizeFlagsHorizontal & SizeFlags.Expand) == SizeFlags.Expand;
                }

                if (!stretch)
                {
                    stretchMin += minSize;
                }
                else
                {
                    totalStretchRatio = child.SizeFlagsStretchRatio;
                }

                sizeList.Add((child, minSize, minSize, stretch));
            }

            int stretchMax;
            if (Vertical)
            {
                stretchMax = (int) Size.Y;
            }
            else
            {
                stretchMax = (int) Size.X;
            }

            stretchMax -= separation * (ChildCount - 1);
            // This is the amount of space allocated for stretchable children.
            var stretchAvail = Math.Max(0, stretchMax - stretchMin);

            // Step two: figure out which that want to stretch need to suck it,
            // because due to their stretch ratio they would be smaller than minSize.
            // Treat those as non-stretching.
            for (var i = 0; i < sizeList.Count; i++)
            {
                var (control, minSize, _, stretch) = sizeList[i];
                if (!stretch)
                {
                    continue;
                }

                var share = (int) (stretchAvail * (control.SizeFlagsStretchRatio / totalStretchRatio));
                if (share < minSize)
                {
                    sizeList[i] = (control, minSize, minSize, false);
                    stretchAvail -= minSize;
                    totalStretchRatio -= control.SizeFlagsStretchRatio;
                }
            }

            // Step three: allocate space for all the stretchable children.
            var stretchingAtAll = false;
            for (var i = 0; i < sizeList.Count; i++)
            {
                var (control, minSize, _, stretch) = sizeList[i];
                if (!stretch)
                {
                    continue;
                }

                stretchingAtAll = true;

                var share = (int) (stretchAvail * (control.SizeFlagsStretchRatio / totalStretchRatio));
                sizeList[i] = (control, minSize, share, false);
            }

            // Step four: actually lay them out one by one.
            var offset = 0;
            if (!stretchingAtAll)
            {
                switch (_align)
                {
                    case AlignMode.Begin:
                        break;
                    case AlignMode.Center:
                        offset = stretchAvail / 2;
                        break;
                    case AlignMode.End:
                        offset = stretchAvail;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            var first = true;
            foreach (var (control, _, size, _) in sizeList)
            {
                if (!first)
                {
                    offset += separation;
                }

                first = false;

                UIBox2 targetBox;
                if (Vertical)
                {
                    targetBox = new UIBox2(0, offset, Size.X, offset+size);
                }
                else
                {
                    targetBox = new UIBox2(offset, 0, offset+size, Size.Y);
                }

                FitChildInBox(control, targetBox);

                offset += size;
            }
        }

        protected override Vector2 CalculateMinimumSize()
        {
            if (GameController.OnGodot)
            {
                return base.CalculateMinimumSize();
            }
            var separation = ActualSeparation;

            var minWidth = 0f;
            var minHeight = 0f;
            var first = true;

            foreach (var child in Children)
            {
                var (childWidth, childHeight) = child.CombinedMinimumSize;
                if (Vertical)
                {
                    minHeight += childHeight;
                    if (!first)
                    {
                        minHeight += separation;
                    }

                    first = false;

                    minWidth = Math.Max(minWidth, childWidth);
                }
                else
                {
                    minWidth += childWidth;
                    if (!first)
                    {
                        minWidth += separation;
                    }

                    first = false;

                    minHeight = Math.Max(minHeight, childHeight);
                }
            }

            return new Vector2(minWidth, minHeight);
        }

        public enum AlignMode
        {
            Begin = 0,
            Center = 1,
            End = 2,
        }
    }
}
