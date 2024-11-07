using System;
using System.Diagnostics.Contracts;
using System.Numerics;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    [Virtual]
    public class ScrollContainer : Container
    {
        private bool _queueScrolled = false;
        private bool _vScrollEnabled = true;
        private bool _hScrollEnabled = true;

        private bool _vScrollVisible;
        private bool _hScrollVisible;

        private readonly VScrollBar _vScrollBar;
        private readonly HScrollBar _hScrollBar;

        private bool _suppressScrollValueChanged;

        /// <summary>
        /// If true then if we have a y-axis scroll it will convert it to an x-axis scroll.
        /// </summary>
        public bool FallbackDeltaScroll { get; set; } = true;

        public int ScrollSpeedX { get; set; } = 50;
        public int ScrollSpeedY { get; set; } = 50;

        private bool _reserveScrollbarSpace;
        public bool ReserveScrollbarSpace
        {
            get => _reserveScrollbarSpace;
            set
            {
                if (value == _reserveScrollbarSpace)
                    return;

                _reserveScrollbarSpace = value;
                _vScrollBar.ReservesSpace = value;
                _hScrollBar.ReservesSpace = value;
            }
        }

        public bool ReturnMeasure { get; set; } = false;

        public event Action? OnScrolled;

        public ScrollContainer()
        {
            MouseFilter = MouseFilterMode.Pass;
            RectClipContent = true;

            Action<Range> ev = _scrollValueChanged;
            _hScrollBar = new HScrollBar
            {
                Visible = _hScrollEnabled,
                VerticalAlignment = VAlignment.Bottom,
                HorizontalAlignment = HAlignment.Stretch
            };
            _vScrollBar = new VScrollBar
            {
                Visible = _vScrollEnabled,
                VerticalAlignment = VAlignment.Stretch,
                HorizontalAlignment = HAlignment.Right
            };
            AddChild(_hScrollBar);
            AddChild(_vScrollBar);
            _hScrollBar.OnValueChanged += ev;
            _vScrollBar.OnValueChanged += ev;
            _vScrollBar.ReservesSpace = ReserveScrollbarSpace;
            _hScrollBar.ReservesSpace = ReserveScrollbarSpace;
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
                size = Vector2.Max(size, child.DesiredSize);
            }

            // Unlike WPF/Avalonia we default to reporting ZERO here instead of available size. This is to fix a bunch
            // of jank with e.g. BoxContainer.
            if (!ReturnMeasure)
                return Vector2.Zero;

            if (_vScrollEnabled && size.Y >= availableSize.Y)
                size.X += _vScrollBar.DesiredSize.X;

            if (_hScrollEnabled && size.X >= availableSize.X)
                size.Y += _hScrollBar.DesiredSize.Y;

            return size;
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

                maxChildMinSize = Vector2.Max(child.DesiredSize, maxChildMinSize);
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

                if (sWidth < cWidth && _hScrollEnabled && !MathHelper.CloseTo(sWidth, cWidth, 1e-3))
                {
                    _hScrollBar.Visible = _hScrollVisible = true;
                    _hScrollBar.Page = sWidth;
                    _hScrollBar.MaxValue = cWidth;
                }
                else
                {
                    _hScrollBar.Visible = _hScrollVisible = false;
                }

                if (sHeight < cHeight && _vScrollEnabled && !MathHelper.CloseTo(sHeight, cHeight, 1e-3))
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

            var realFinalSize = new Vector2(
                _hScrollEnabled ? Math.Max(cWidth, sWidth) : sWidth,
                _vScrollEnabled ? Math.Max(cHeight, sHeight) : sHeight);

            foreach (var child in Children)
            {
                if (child == _vScrollBar || child == _hScrollBar)
                {
                    continue;
                }

                var position = -GetScrollValue();
                var rect = UIBox2.FromDimensions(position, realFinalSize);
                child.Arrange(rect);
            }

            return finalSize;
        }

        protected override void ArrangeCore(UIBox2 finalRect)
        {
            base.ArrangeCore(finalRect);

            if (!_queueScrolled) return;

            OnScrolled?.Invoke();
            _queueScrolled = false;
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
                var delta =
                    args.Delta.X == 0f &&
                    !_vScrollEnabled &&
                    FallbackDeltaScroll ?
                        -args.Delta.Y :
                        args.Delta.X;

                _hScrollBar.ValueTarget += delta * ScrollSpeedX;
            }

            if (!_vScrollVisible && !_hScrollVisible)
                return;

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
        public Vector2 GetScrollValue(bool ignoreVisible = false)
        {
            if (ignoreVisible)
                return new(_hScrollBar.Value, _vScrollBar.Value);

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
            _queueScrolled = true;
        }

        #region Scrollbat Set Methods

        /// <summary>
        /// Sets the scrollbars thumbs position to a value from 1 (the start of the scrollbar) to 100 (the end of the scrollbar)
        /// </summary>
        /// <param name="value">
        /// The value from 1 to 100 that we want to put the scrollbars thumbs in.
        /// Any value higher than 100 will just be interpreted as 100
        /// </param>
        public void SetScrollValueToPercentage(int? hValue, int? vValue)
        {
            if (hValue != null)
                SetHScrollValueToPercentage(hValue.Value);
            if (vValue != null)
                SetVScrollValueToPercentage(vValue.Value);
        }

        /// <summary>
        /// Sets the horizontal scrollbar thumb position to a value from 1 (the start of the scrollbar) to 100 (the end of the scrollbar)
        /// </summary>
        /// <param name="value">
        /// The value from 1 to 100 that we want to put the scrollbar thumb in.
        /// Any value higher than 100 will just be interpreted as 100
        /// </param>
        public void SetHScrollValueToPercentage(int value)
        {
            var size = _hScrollBar.MaxValue - _hScrollBar.Page;
            var target = (float) (0.01 * value * size);
            SetScrollValue(target, null);
        }

        /// <summary>
        /// Sets the vertical scrollbar thumb position to a value from 1 (the start of the scrollbar) to 100 (the end of the scrollbar)
        /// </summary>
        /// <param name="value">
        /// The value from 1 to 100 that we want to put the scrollbar thumb in.
        /// Any value higher than 100 will just be interpreted as 100
        /// </param>
        public void SetVScrollValueToPercentage(int value)
        {
            var size = _vScrollBar.MaxValue - _vScrollBar.Page;
            var target = (float) (0.01 * value * size);
            SetScrollValue(null, target);
        }

        public void SetScrollValue(Vector2 value) => SetScrollValue(value.X, value.Y);

        public void SetScrollValue(float? hValue, float? vValue)
        {
            _suppressScrollValueChanged = true;
            if (hValue != null)
                _hScrollBar.Value = hValue.Value;
            if (vValue != null)
                _vScrollBar.Value = vValue.Value;
            _suppressScrollValueChanged = false;
            InvalidateArrange();
            _queueScrolled = true;
        }

        #endregion
    }
}
