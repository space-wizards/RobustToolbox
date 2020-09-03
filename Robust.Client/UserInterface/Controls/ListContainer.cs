using Robust.Shared.Maths;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Robust.Client.UserInterface.Controls
{
    public class ListContainer : Container
    {
        public const string StylePropertySeparation = "separation";

        private const int DefaultSeparation = 0;

        public readonly VScrollBar VScrollBar;
        private readonly List<Control> _list;
        private readonly List<float> _heights;
        private float _totalHeight;
        private bool _updatePosition;
        private bool _disableListRemove;
        private bool _updateChildren;
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

        public int StartIndex { get; private set; }
        public int EndIndex { get; private set; }

        public int ItemCount => _list.Count;

        public int? SeparationOverride { get; set; }

        public ListContainer() : base()
        {
            MouseFilter = MouseFilterMode.Pass;
            RectClipContent = true;

            _list = new List<Control>();
            _heights = new List<float>();

            VScrollBar = new VScrollBar
            {
                Visible = false,
                SizeFlagsVertical = SizeFlags.Fill,
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd
            };

            AddChild(VScrollBar);
            VScrollBar.OnValueChanged += _scrollValueChanged;

            StartIndex = -1;
            EndIndex = -1;
            _disableListRemove = false;
            _updateChildren = false;
            _disableListCalc = false;
        }

        public Control? GetLastItem()
        {
            return _list.Count > 0 ? _list[_list.Count - 1] : null;
        }

        protected override void LayoutUpdateOverride()
        {
            if (VScrollBar?.Parent == null)
            {
                // Just don't run this before we're properly initialized.
                return;
            }

            DebugTools.Assert(_list.Count == _heights.Count);

            // Calculate _vScrollBar height
            var separation = (int)(ActualSeparation * UIScale);
            var (sWidth, sHeight) = Size;

            RecalculateScrollBar();

            if (VScrollBar.Visible)
            {
                sWidth -= VScrollBar.Width;
            }

            FitChildInPixelBox(VScrollBar, PixelSizeBox);

            // Calculate which items are to be shown
            if (_list.Count == 0)
            {
                return;
            }

            /*
             * Example:
             * 
             * height | _heights | Type
             * 
             * 10 | 00 | Control.Size.Y
             * 10 | 10 | Control.Size.Y
             * 10 | 20 | Control.Size.Y
             * 
             * If viewport height is 20
             * visible should be 2 items (start = 0, end = 1)
             * 
             * scroll.Y = 5
             * visible should be 3 items (start = 0, end = 2)
             * 
             * start:
             * scroll.Y >= _heights[start + 1]
             * 5 >= _heights[0 + 1]
             * 5 >= 10
             * so start = 0
             * 
             * end:
             * scroll.Y + Height > _heights[end + 1]
             * 5 + 20 > _heights[1 + 1]
             * 25 > 20
             * so end = 2
             */
            var start = StartIndex;
            var scroll = _getScrollValue();
            // If scroll is past the *next* threshold then increase start.
            while (start < _heights.Count - 1 && scroll.Y >= _heights[start + 1] + (separation * start))
            {
                start += 1;
            }
            // If scroll is less than the current height, thus going into the previous limit, decrease start
            while (start > 0 && scroll.Y < _heights[start] + (separation * start))
            {
                start -= 1;
            }

            // When scrolling only rebuild visible list when a new item should be visible
            if (start != StartIndex)
            {
                StartIndex = start;
                _updateChildren = true;
            }

            // Do the same as above for the bottom of the list, but 
            var end = EndIndex;
            var visibleBottom = scroll.Y + Height;
            while (end < _heights.Count - 1 && visibleBottom > _heights[end + 1] + (separation * end))
            {
                end += 1;
            }
            while (end > 0 && visibleBottom <= _heights[end] + (separation * end))
            {
                end -= 1;
            }

            if (end != EndIndex)
            {
                EndIndex = end;
                _updateChildren = true;
            }

            if (_updateChildren)
            {
                _updateChildren = false;

                // _disableListRemove is so that only Children and not _list is changed
                _disableListRemove = true;
                foreach (var child in Children.ToList())
                {
                    if (child == VScrollBar)
                    {
                        continue;
                    }

                    RemoveChild(child);
                }
                _disableListRemove = false;

                for (var i = StartIndex; i <= EndIndex; i++)
                {
                    AddChild(_list[i]);
                }

                VScrollBar.SetPositionLast();
            }

            if (ChildCount <= 1 || !_updatePosition)
            {
                return;
            }

            var offset = _heights[StartIndex] + (separation * StartIndex) - (float)Math.Round(scroll.Y);

            foreach (var child in Children)
            {
                if (child == VScrollBar)
                {
                    continue;
                }

                var (x, y) = child.CombinedMinimumSize;
                var targetBox = UIBox2.FromDimensions(0, offset, sWidth, y);
                FitChildInBox(child, targetBox);

                //var size = child.CombinedMinimumSize.Y;
                offset += y + separation;
            }
        }

        private void RecalculateScrollBar()
        {
            var separation = (int)(ActualSeparation * UIScale);
            var cHeight = _totalHeight + (separation * _heights.Count);
            var (sWidth, sHeight) = Size;

            try
            {
                // Suppress events to avoid weird recursion.
                _suppressScrollValueChanged = true;

                if (sHeight < cHeight)
                {
                    VScrollBar.Visible = true;
                    VScrollBar.Page = sHeight;
                    VScrollBar.MaxValue = cHeight;
                }
                else
                {
                    VScrollBar.Visible = false;
                }
            }
            finally
            {
                // I really don't think this can throw an exception but oh well let's finally it.
                _suppressScrollValueChanged = false;
            }
        }

        private void OnChildMinimumSizeChanged(Control child)
        {
            RecalculateListHeight(_list.IndexOf(child));
        }

        // Should be updated every time _list changes and when the size of an item in the _list changes
        private void RecalculateListHeight(int index)
        {
            // Ensure _heights has the same count as _list
            if (_heights.Count > _list.Count)
            {
                _heights.RemoveRange(_list.Count, _heights.Count - _list.Count);
                if (EndIndex >= _heights.Count)
                {
                    EndIndex = _heights.Count - 1;
                }
            }
            else
            {
                while (_heights.Count < _list.Count)
                {
                    _heights.Add(0);
                }
            }

            if (_disableListCalc || index < 0 || index >= _heights.Count)
            {
                return;
            }

            _disableListCalc = true;

            var i = index > 0 ? index - 1 : 0;
            var height = _heights[i];
            if (_heights.Count > 0 && _heights[i] + _list[index].CombinedMinimumSize.Y == _heights[index])
            {
                _disableListCalc = false;
                return;
            }

            for (; i < _list.Count; i++)
            {
                _heights[i] = height;
                height += _list[i].CombinedMinimumSize.Y;
            }

            _disableListCalc = false;

            _totalHeight = height;
            _updatePosition = true;

            RecalculateScrollBar();
        }

        protected internal override void MouseWheel(GUIMouseWheelEventArgs args)
        {
            base.MouseWheel(args);

            VScrollBar.ValueTarget -= args.Delta.Y * 50;
        }

        protected override void ChildAdded(Control newChild)
        {
            base.ChildAdded(newChild);

            if (newChild == VScrollBar)
            {
                if (VScrollBar?.Parent == null)
                {
                    // Just don't run this before we're properly initialized.
                    return;
                }

                VScrollBar?.SetPositionLast();
                return;
            }

            if (_list.Contains(newChild))
            {
                return;
            }

            _list.Add(newChild);
            RecalculateListHeight(_list.Count - 1);

            //AddItem(newChild);
            newChild.OnMinimumSizeChanged += OnChildMinimumSizeChanged;

            _updateChildren = true;
            //if (_vScrollBar?.MaxValue > Height)
            //{
            //    _disableListRemove = true;
            //    RemoveChild(newChild);
            //    _disableListRemove = false;
            //}
        }

        public void AddItem(Control newChild)
        {
        }

        protected override void ChildRemoved(Control child)
        {
            if (_disableListRemove)
            {
                return;
            }

            RemoveItem(child);

            base.ChildRemoved(child);
        }

        public void RemoveItem(Control child)
        {
            if (!_list.Contains(child))
            {
                return;
            }

            var index = _list.IndexOf(child);
            _list.Remove(child);
            _updateChildren = true;
            child.OnMinimumSizeChanged -= OnChildMinimumSizeChanged;

            if (Children.Contains(child))
            {
                RemoveChild(child);
            }
            RecalculateListHeight(index);
            UpdateLayout();
        }

        [Pure]
        private Vector2 _getScrollValue()
        {
            var v = VScrollBar.Value;
            if (!VScrollBar.Visible)
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

            _updatePosition = true;
            UpdateLayout();
        }
    }
}
