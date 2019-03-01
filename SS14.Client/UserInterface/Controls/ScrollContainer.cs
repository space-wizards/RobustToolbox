using System;
using System.Diagnostics.Contracts;
using SS14.Client.Input;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.ScrollContainer))]
    public class ScrollContainer : Container
    {
        private bool _vScrollEnabled = true;
        private bool _hScrollEnabled = true;

        private bool _vScrollVisible;
        private bool _hScrollVisible;

        private VScrollBar _vScrollBar;
        private HScrollBar _hScrollBar;

        private bool _suppressScrollValueChanged;

        public ScrollContainer()
        {
        }

        public ScrollContainer(string name) : base(name)
        {
        }

        internal ScrollContainer(Godot.ScrollContainer container) : base(container)
        {
        }

        public bool VScrollEnabled
        {
            get => _vScrollEnabled;
            set
            {
                _vScrollEnabled = value;
                MinimumSizeChanged();
            }
        }

        public bool HScrollEnabled
        {
            get => _hScrollEnabled;
            set
            {
                _hScrollEnabled = value;
                MinimumSizeChanged();
            }
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.ScrollContainer();
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (GameController.OnGodot)
            {
                return;
            }

            Action<Range> ev = _scrollValueChanged;
            _hScrollBar = new HScrollBar {Visible = false};
            _vScrollBar = new VScrollBar {Visible = false};
            AddChild(_hScrollBar);
            AddChild(_vScrollBar);
            _hScrollBar.SetAnchorAndMarginPreset(LayoutPreset.BottomWide);
            _vScrollBar.SetAnchorAndMarginPreset(LayoutPreset.RightWide);
            _hScrollBar.OnValueChanged += ev;
            _vScrollBar.OnValueChanged += ev;
        }

        protected override void SortChildren()
        {
            if (_vScrollBar?.Parent == null || _hScrollBar?.Parent == null)
            {
                // Just don't run this before we're properly initialized.
                return;
            }
            var maxChildMinSize = Vector2.Zero;

            foreach (var child in Children)
            {
                if (child == _vScrollBar || child == _hScrollBar)
                {
                    continue;
                }

                maxChildMinSize = Vector2.ComponentMax(child.CombinedMinimumSize, maxChildMinSize);
            }

            var (cWidth, cHeight) = maxChildMinSize;
            var hBarSize = _hScrollBar.CombinedMinimumSize.Y;
            var vBarSize = _vScrollBar.CombinedMinimumSize.X;
            var sSize = Size - new Vector2(hBarSize, vBarSize);
            var (sWidth, sHeight) = sSize;

            try
            {
                // Suppress events to avoid weird recursion.
                _suppressScrollValueChanged = true;

                if (sWidth < cWidth)
                {
                    _hScrollBar.Visible = _hScrollVisible = true;
                    _hScrollBar.Page = sWidth;
                    _hScrollBar.MaxValue = cWidth;
                }
                else
                {
                    _hScrollBar.Visible = _hScrollVisible = false;
                }

                if (sHeight < cHeight)
                {
                    _vScrollBar.Visible = _vScrollVisible = true;
                    _vScrollBar.Page = sHeight;
                    _vScrollBar.MaxValue = cHeight;
                }
                else
                {
                    _vScrollBar.Visible = _vScrollVisible = false;
                }
            }
            finally
            {
                // I really don't think this can throw an exception but oh well let's finally it.
                _suppressScrollValueChanged = false;
            }

            _vScrollBar.SetPositionLast();
            _hScrollBar.SetPositionLast();

            foreach (var child in Children)
            {
                if (child == _vScrollBar || child == _hScrollBar)
                {
                    continue;
                }

                var position = -_getScrollValue();
                var rect = UIBox2.FromDimensions(position, Vector2.ComponentMax(child.CombinedMinimumSize, sSize));
                FitChildInBox(child, rect);
            }
        }

        protected override Vector2 CalculateMinimumSize()
        {
            if (GameController.OnGodot)
            {
                return Vector2.Zero;
            }

            var totalX = 0f;
            var totalY = 0f;

            foreach (var child in Children)
            {
                if (child == _hScrollBar || child == _vScrollBar)
                {
                    continue;
                }

                if (!_vScrollEnabled)
                {
                    totalY = Math.Max(totalY, child.CombinedMinimumSize.Y);
                }

                if (!_hScrollEnabled)
                {
                    totalX = Math.Max(totalX, child.CombinedMinimumSize.X);
                }
            }

            if (_vScrollEnabled)
            {
                totalX += _vScrollBar.CombinedMinimumSize.X;
                totalY = Math.Max(_vScrollBar.CombinedMinimumSize.Y, totalY);
            }

            if (_hScrollEnabled)
            {
                totalY += _hScrollBar.CombinedMinimumSize.Y;
                totalX = Math.Max(_vScrollBar.CombinedMinimumSize.X, totalX);
            }

            return new Vector2(totalX, totalY);
        }

        protected override void SetDefaults()
        {
            base.SetDefaults();

            RectClipContent = true;
        }

        protected internal override void MouseWheel(GUIMouseWheelEventArgs args)
        {
            base.MouseWheel(args);

            var up = args.WheelDirection == Mouse.Wheel.Up;

            if (_vScrollEnabled)
            {
                if (up)
                {
                    _vScrollBar.Value -= 10;
                }
                else
                {
                    _vScrollBar.Value += 10;
                }
            }
        }

        [Pure]
        private Vector2 _getScrollValue()
        {
            var h = _hScrollBar.Value;
            var v = _vScrollBar.Value;
            if (!_hScrollVisible)
            {
                h = 0;
            }
            if (!_vScrollVisible)
            {
                v = 0;
            }
            return new Vector2(h, v);
        }

        private void _scrollValueChanged(Range obj)
        {
            if (_suppressScrollValueChanged)
            {
                return;
            }

            SortChildren();
        }
    }
}
