using Robust.Shared.Maths;
using Robust.Shared.Utility;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Robust.Client.UserInterface.Controls
{
    public class ListContainer : Container
    {
        public const string StylePropertySeparation = "separation";

        private const int DefaultSeparation = 0;

        private readonly VScrollBar _vScrollBar;
        private readonly List<Control> _list;
        private readonly List<float> _heights;
        private int _startIndex;
        private int _endIndex;
        private bool _disableListRemove;
        private bool _dirty;
        private bool _disableListCalc;

        private bool _suppressScrollValueChanged;
        private int ActualSeparation
        {
            get
            {
                if (TryGetStyleProperty(StylePropertySeparation, out int separation))
                {
                    return separation;
                }

                return SeparationOverride ?? DefaultSeparation;
            }
        }

        public int? SeparationOverride { get; set; }

        public ListContainer() : base()
        {
            MouseFilter = MouseFilterMode.Pass;
            RectClipContent = true;

            _list = new List<Control>();
            _heights = new List<float>();

            _vScrollBar = new VScrollBar
            {
                Visible = false,
                SizeFlagsVertical = SizeFlags.Fill,
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd
            };

            AddChild(_vScrollBar);
            _vScrollBar.OnValueChanged += _scrollValueChanged;

            _startIndex = 0;
            _disableListRemove = false;
            _dirty = true;
            _disableListCalc = false;
        }

        public void RemoveItem(Control child)
        {
            if (!_list.Contains(child))
            {
                return;
            }

            var index = _list.IndexOf(child);
            _list.Remove(child);

            if (Children.Contains(child))
            {
                RemoveChild(child);
            }

            RecalculateListHeight(index);
        }

        public Control? GetLastItem()
        {
            return _list.Count > 0 ? _list[_list.Count - 1] : null;
        }

        protected override void LayoutUpdateOverride()
        {
            if (_vScrollBar?.Parent == null)
            {
                // Just don't run this before we're properly initialized.
                return;
            }

            DebugTools.Assert(_list.Count == _heights.Count);

            var separation = (int)(ActualSeparation * UIScale);

            var start = _startIndex;
            var scroll = _getScrollValue();
            while (start < _heights.Count - 1 && scroll.Y >= _heights[start + 1] + (separation * start))
            {
                start += 1;
            }
            while (start > 0 && scroll.Y < _heights[start] + (separation * start))
            {
                start -= 1;
            }

            if (start != _startIndex)
            {
                _startIndex = start;
                _dirty = true;
            }

            var end = _endIndex;
            while (end < _heights.Count - 1 && scroll.Y + Height >= _heights[end + 1] + (separation * end))
            {
                end += 1;
            }
            while (end > 0 && scroll.Y + Height < _heights[end] + (separation * end))
            {
                end -= 1;
            }

            if (end != _endIndex)
            {
                _endIndex = end;
                _dirty = true;
            }

            if (_dirty)
            {
                _dirty = false;

                _disableListRemove = true;
                foreach (var child in Children.ToList())
                {
                    if (child == _vScrollBar)
                    {
                        continue;
                    }

                    RemoveChild(child);
                }
                _disableListRemove = false;

                for (var i = _startIndex; i <= _endIndex; i++)
                {
                    AddChild(_list[i]);
                }
            }

            var cHeight = (_heights.Count > 0 ? _heights[_heights.Count - 1] : 0) + (separation * _heights.Count);

            var (sWidth, sHeight) = Size;

            try
            {
                // Suppress events to avoid weird recursion.
                _suppressScrollValueChanged = true;

                if (sHeight < cHeight)
                {
                    _vScrollBar.Visible = true;
                    _vScrollBar.Page = sHeight;
                    _vScrollBar.MaxValue = cHeight;
                }
                else
                {
                    _vScrollBar.Visible = false;
                }
            }
            finally
            {
                // I really don't think this can throw an exception but oh well let's finally it.
                _suppressScrollValueChanged = false;
            }

            FitChildInPixelBox(_vScrollBar, PixelSizeBox);

            var sSize = (sWidth, sHeight);
            var offset = new Vector2(0, _heights[_startIndex] + (separation * _startIndex)) - scroll;

            foreach (var child in Children)
            {
                if (child == _vScrollBar)
                {
                    continue;
                }

                var targetBox = UIBox2.FromDimensions(offset, Vector2.ComponentMax(child.CombinedMinimumSize, sSize));
                FitChildInBox(child, targetBox);

                var size = child.CombinedMinimumSize.Y;
                offset += new Vector2(0, size + separation);
            }
        }

        private void OnChildMinimumSizeChanged(Control child)
        {
            if (child == _vScrollBar)
            {
                return;
            }

            RecalculateListHeight(_list.IndexOf(child));
        }

        private void RecalculateListHeight(int index)
        {
            _heights.RemoveRange(_list.Count, _heights.Count - _list.Count);
            if (_endIndex >= _heights.Count)
            {
                _endIndex = _heights.Count - 1;
            }
            if (_disableListCalc || index < 0 || index >= _list.Count)
            {
                return;
            }

            _disableListCalc = true;

            var i = index;
            var height = _heights.Count > 0 ? _heights[i] : 0f;
            for (; i < _list.Count; i++)
            {
                if (_heights.Count <= i)
                {
                    _heights.Add(height);
                }
                else
                {
                    _heights[i] = height;
                }
                height += _list[i].CombinedMinimumSize.Y;
            }

            _disableListCalc = false;
        }

        protected internal override void MouseWheel(GUIMouseWheelEventArgs args)
        {
            base.MouseWheel(args);

            _vScrollBar.ValueTarget -= args.Delta.Y * 50;
        }

        protected override void ChildAdded(Control newChild)
        {
            base.ChildAdded(newChild);

            if (newChild == _vScrollBar)
            {
                if (_vScrollBar?.Parent == null)
                {
                    // Just don't run this before we're properly initialized.
                    return;
                }

                _vScrollBar?.SetPositionLast();
                return;
            }

            if (_list.Contains(newChild))
            {
                return;
            }

            _list.Add(newChild);
            _heights.Add(0);
            newChild.OnMinimumSizeChanged += OnChildMinimumSizeChanged;

            if (_vScrollBar?.MaxValue > Height)
            {
                _disableListRemove = true;
                RemoveChild(newChild);
                _disableListRemove = false;
            }
        }

        protected override void ChildRemoved(Control child)
        {
            if (_disableListRemove)
            {
                return;
            }

            base.ChildRemoved(child);

            child.OnMinimumSizeChanged -= OnChildMinimumSizeChanged;

            var index = _list.IndexOf(child);

            if (_list.Remove(child))
            {
                RecalculateListHeight(index);
            }
        }

        [Pure]
        private Vector2 _getScrollValue()
        {
            var v = _vScrollBar.Value;
            if (!_vScrollBar.Visible)
            {
                v = 0;
            }
            return new Vector2(0, v);
        }

        private void _scrollValueChanged(Range obj)
        {
            if (_suppressScrollValueChanged)
            {
                return;
            }

            UpdateLayout();
        }
    }
}
