using System;
using System.Diagnostics.Contracts;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    public class ScrollContainer : Container
    {
        private bool _vScrollEnabled = true;
        private bool _hScrollEnabled = true;

        private bool _vScrollVisible;
        private bool _hScrollVisible;

        private readonly VScrollBar _vScrollBar;
        private readonly HScrollBar _hScrollBar;

        private bool _suppressScrollValueChanged;

        public int ScrollSpeedX { get; set; } = 50;
        public int ScrollSpeedY { get; set; } = 50;

        public bool ReturnMeasure { get; set; } = false;

        public ScrollContainer()
        {
            MouseFilter = MouseFilterMode.Pass;
            RectClipContent = true;

            Action<Range> ev = _scrollValueChanged;
            _hScrollBar = new HScrollBar
            {
                Visible = false,
                VerticalAlignment = VAlignment.Bottom,
                HorizontalAlignment = HAlignment.Stretch
            };
            _vScrollBar = new VScrollBar
            {
                Visible = false,
                VerticalAlignment = VAlignment.Stretch,
                HorizontalAlignment = HAlignment.Right
            };
            AddChild(_hScrollBar);
            AddChild(_vScrollBar);
            _hScrollBar.OnValueChanged += ev;
            _vScrollBar.OnValueChanged += ev;
        }

        public bool VScrollEnabled
        {
            get => _vScrollEnabled;
            set
            {
                _vScrollEnabled = value;
                InvalidateMeasure();
            }
        }

        public bool HScrollEnabled
        {
            get => _hScrollEnabled;
            set
            {
                _hScrollEnabled = value;
                InvalidateMeasure();
            }
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            if (_vScrollEnabled)
            {
                _vScrollBar.Measure(availableSize);
                availableSize.X -= _vScrollBar.DesiredSize.X;
            }

            if (_hScrollEnabled)
            {
                _hScrollBar.Measure(availableSize);
                availableSize.Y -= _hScrollBar.DesiredSize.Y;
            }

            var constraint = new Vector2(
                _hScrollEnabled ? float.PositiveInfinity : availableSize.X,
                _vScrollEnabled ? float.PositiveInfinity : availableSize.Y);

            var size = Vector2.Zero;
            foreach (var child in Children)
            {
                child.Measure(constraint);
                size = Vector2.ComponentMax(size, child.DesiredSize);
            }

            // Unlike WPF/Avalonia we default to reporting ZERO here instead of available size. This is to fix a bunch
            // of jank with e.g. BoxContainer.
            return ReturnMeasure ? size : Vector2.Zero;
        }

        protected override Vector2 ArrangeOverride(Vector2 finalSize)
        {
            if (_vScrollBar?.Parent == null || _hScrollBar?.Parent == null)
            {
                // Just don't run this before we're properly initialized.
                return Vector2.Zero;
            }

            var maxChildMinSize = Vector2.Zero;

            foreach (var child in Children)
            {
                if (child == _vScrollBar || child == _hScrollBar)
                {
                    continue;
                }

                maxChildMinSize = Vector2.ComponentMax(child.DesiredSize, maxChildMinSize);
            }

            var (cWidth, cHeight) = maxChildMinSize;
            var hBarSize = _hScrollBar.DesiredSize.Y;
            var vBarSize = _vScrollBar.DesiredSize.X;

            var (sWidth, sHeight) = finalSize;

            try
            {
                // Suppress events to avoid weird recursion.
                _suppressScrollValueChanged = true;

                if (sWidth < cWidth && _hScrollEnabled)
                {
                    sHeight -= hBarSize;
                }

                if (sHeight < cHeight && _vScrollEnabled)
                {
                    sWidth -= vBarSize;
                }

                if (sWidth < cWidth && _hScrollEnabled)
                {
                    _hScrollBar.Visible = _hScrollVisible = true;
                    _hScrollBar.Page = sWidth;
                    _hScrollBar.MaxValue = cWidth;
                }
                else
                {
                    _hScrollBar.Visible = _hScrollVisible = false;
                }

                if (sHeight < cHeight && _vScrollEnabled)
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

            if (_vScrollVisible)
            {
                _vScrollBar.Arrange(UIBox2.FromDimensions(Vector2.Zero, finalSize));
            }

            if (_hScrollVisible)
            {
                _hScrollBar.Arrange(UIBox2.FromDimensions(Vector2.Zero, finalSize));
            }

            var realFinalSize = (
                _hScrollEnabled ? Math.Max(cWidth, sWidth) : sWidth,
                _vScrollEnabled ? Math.Max(cHeight, sHeight) : sHeight);

            foreach (var child in Children)
            {
                if (child == _vScrollBar || child == _hScrollBar)
                {
                    continue;
                }

                var position = -_getScrollValue();
                var rect = UIBox2.FromDimensions(position, realFinalSize);
                child.Arrange(rect);
            }

            return finalSize;
        }

        protected internal override void MouseWheel(GUIMouseWheelEventArgs args)
        {
            base.MouseWheel(args);

            if (_vScrollEnabled)
            {
                _vScrollBar.ValueTarget -= args.Delta.Y * ScrollSpeedY;
            }

            if (_hScrollEnabled)
            {
                _hScrollBar.ValueTarget += args.Delta.X * ScrollSpeedX;
            }

            args.Handle();
        }

        protected override void ChildAdded(Control newChild)
        {
            base.ChildAdded(newChild);

            if (_vScrollBar?.Parent == null || _hScrollBar?.Parent == null)
            {
                // Just don't run this before we're properly initialized.
                return;
            }

            _vScrollBar?.SetPositionLast();
            _hScrollBar?.SetPositionLast();
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

            InvalidateArrange();
        }
    }
}
