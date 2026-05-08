using System;
using System.Diagnostics.Contracts;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Collections;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     A control to handle output of message-by-message output panels, like the debug console and chat panel.
    /// </summary>
    [Virtual]
    public class OutputPanel : SelectableTextControl
    {
        public const string StyleClassOutputPanelScrollDownButton = "outputPanelScrollDownButton";

        [Dependency] private readonly MarkupTagManager _tagManager = default!;

        public const string StylePropertyStyleBox = "stylebox";

        public bool ShowScrollDownButton
        {
            get => _showScrollDownButton;
            set
            {
                _showScrollDownButton = value;
                _updateScrollButtonVisibility();
            }
        }
        private bool _showScrollDownButton;

        private readonly RingBufferList<RichTextEntry> _entries = new();
        private bool _isAtBottom = true;

        private int _totalContentHeight;
        private bool _firstLine = true;
        private StyleBox? _styleBoxOverride;
        private VScrollBar _scrollBar;
        private Button _scrollDownButton;
        private int? _selectedEntryIndex;
        private readonly OutputPanelSelectionLayout _selectionLayout;
        private string _copyCache = string.Empty;

        public bool ScrollFollowing { get; set; } = true;

        private bool _invalidOnVisible;

        public OutputPanel()
        {
            IoCManager.InjectDependencies(this);
            MouseFilter = MouseFilterMode.Pass;
            Copyable = true;
            RectClipContent = true;

            _scrollBar = new VScrollBar
            {
                Name = "_v_scroll",
                HorizontalAlignment = HAlignment.Right
            };
            AddChild(_scrollBar);

            AddChild(_scrollDownButton = new Button()
            {
                Name = "scrollLiveBtn",
                StyleClasses = { StyleClassOutputPanelScrollDownButton },
                VerticalAlignment = VAlignment.Bottom,
                HorizontalAlignment = HAlignment.Center,
                Text = String.Format("⬇    {0}    ⬇", Loc.GetString("output-panel-scroll-down-button-text")),
                MaxWidth = 300,
                Visible = false,
            });

            _scrollDownButton.OnPressed += _ => ScrollToBottom();

            _scrollBar.OnValueChanged += _ =>
            {
                _isAtBottom = _scrollBar.IsAtEnd;
                _updateScrollButtonVisibility();
            };

            _selectionLayout = new OutputPanelSelectionLayout(this);
        }

        public int EntryCount => _entries.Count;

        public StyleBox? StyleBoxOverride
        {
            get => _styleBoxOverride;
            set
            {
                _styleBoxOverride = value;
                InvalidateMeasure();
                _invalidateEntries();
            }
        }

        public void Clear()
        {
            _firstLine = true;

            foreach (var entry in _entries)
            {
                entry.RemoveControls();
            }

            _entries.Clear();
            _totalContentHeight = 0;
            _scrollBar.MaxValue = Math.Max(_scrollBar.Page, _totalContentHeight);
            _scrollBar.Value = 0;
            ClearSelectionState();
        }

        public FormattedMessage GetMessage(Index index)
        {
            return new FormattedMessage(_entries[index].Message);
        }

        public void RemoveEntry(Index index)
        {
            var entry = _entries[index];
            entry.RemoveControls();
            _entries.RemoveAt(index.GetOffset(_entries.Count));

            var font = _getFont();
            _totalContentHeight -= entry.Height + font.GetLineSeparation(UIScale);
            if (_entries.Count == 0)
            {
                Clear();
            }

            _scrollBar.MaxValue = Math.Max(_scrollBar.Page, _totalContentHeight);
            ClearSelectionState();
        }

        public void AddText(string text)
        {
            var msg = new FormattedMessage();
            msg.AddText(text);
            AddMessage(msg);
        }

        public void AddMessage(FormattedMessage message, Color? defaultColor = null)
        {
            AddMessage(message, RichTextEntry.DefaultTags, defaultColor);
        }

        public void AddMessage(FormattedMessage message, Type[]? tagsAllowed, Color? defaultColor = null)
        {
            var entry = new RichTextEntry(message, this, _tagManager, tagsAllowed, defaultColor);

            entry.Update(_tagManager, _getFont(), _getContentBox().Width, UIScale);

            _entries.Add(entry);
            var font = _getFont();
            AddNewItemHeight(font, entry);

            _scrollBar.MaxValue = Math.Max(_scrollBar.Page, _totalContentHeight);
            if (_isAtBottom && ScrollFollowing)
            {
                _scrollBar.MoveToEnd();
            }
        }

        public void SetMessage(Index index, FormattedMessage message, Color? defaultColor = null)
        {
            SetMessage(index, message, RichTextEntry.DefaultTags, defaultColor);
        }

        public void SetMessage(Index index, FormattedMessage message, Type[]? tagsAllowed, Color? defaultColor = null)
        {
            var atBottom = !_scrollDownButton.Visible;
            var oldEntry = _entries[index];
            var font = _getFont();
            _totalContentHeight -= oldEntry.Height + font.GetLineSeparation(UIScale);
            _scrollBar.MaxValue = Math.Max(_scrollBar.Page, _totalContentHeight);

            var entry = new RichTextEntry(message, this, _tagManager, tagsAllowed, defaultColor);
            entry.Update(_tagManager, _getFont(), _getContentBox().Width, UIScale);
            _entries[index] = entry;

            AddNewItemHeight(font, in entry);

            _scrollBar.MaxValue = Math.Max(_scrollBar.Page, _totalContentHeight);
            if (atBottom)
                _scrollBar.Value = _scrollBar.MaxValue;

            ClearSelectionState();
        }

        private void AddNewItemHeight(Font font, in RichTextEntry entry)
        {
            _totalContentHeight += entry.Height;
            if (_firstLine)
            {
                _firstLine = false;
            }
            else
            {
                _totalContentHeight += font.GetLineSeparation(UIScale);
            }
        }

        public void ScrollToBottom()
        {
            _scrollBar.MoveToEnd();
            _isAtBottom = true;
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            var style = _getStyleBox();
            var font = _getFont();
            var lineSeparation = font.GetLineSeparation(UIScale);
            style?.Draw(handle, PixelSizeBox, UIScale);
            var contentBox = _getContentBox();

            var entryOffset = -_scrollBar.Value;

            // A stack for format tags.
            // This stack contains the format tag to RETURN TO when popped off.
            // So when a new color tag gets hit this stack gets the previous color pushed on.
            var context = new MarkupDrawingContext(2);

            DrawSelectionIfNeeded(handle);

            var entryIndex = 0;
            foreach (ref var entry in _entries)
            {
                if (entryOffset + entry.Height < 0)
                {
                    // Controls within the entry are the children of this control, which means they are drawn separately
                    // after this Draw call, so we have to mark them as invisible to prevent them from being drawn.
                    //
                    // An alternative option is to ensure that the control position updating logic in entry.Draw is always
                    // run, and then setting RectClipContent = true to use scissor box testing to handle the controls
                    // visibility
                    entry.HideControls();
                    entryOffset += entry.Height + lineSeparation;
                    entryIndex++;
                    continue;
                }

                if (entryOffset > contentBox.Height)
                {
                    entry.HideControls();

                    // We know that every subsequent entry will also fail the test, but we also need to
                    // hide all the controls, so we cannot simply break out of the loop
                    entryIndex++;
                    continue;
                }

                entry.Draw(_tagManager, handle, font, contentBox, entryOffset, context, UIScale);

                entryOffset += entry.Height + lineSeparation;
                entryIndex++;
            }
        }

        protected internal override void MouseWheel(GUIMouseWheelEventArgs args)
        {
            base.MouseWheel(args);

            if (MathHelper.CloseToPercent(0, args.Delta.Y))
            {
                return;
            }

            _scrollBar.ValueTarget -= _getScrollSpeed() * args.Delta.Y;
        }

        protected override ISelectableTextLayout SelectionLayout => _selectionLayout;

        protected override Vector2 ClampSelectionPosition(Vector2 relativePosition)
        {
            if (IsSelecting && _selectedEntryIndex is { } entryIndex && entryIndex < _entries.Count)
            {
                var entryOffset = GetEntryOffset(entryIndex);
                var contentBox = _getContentBox();
                var topPx = contentBox.Top + entryOffset;
                var bottomPx = topPx + _entries[entryIndex].Height;
                var leftPx = contentBox.Left;
                var rightPx = contentBox.Right;

                var posPx = relativePosition * UIScale;
                if (posPx.Y < topPx)
                    posPx = new Vector2(leftPx, topPx);
                else if (posPx.Y > bottomPx)
                    posPx = new Vector2(rightPx, bottomPx);

                posPx.X = MathHelper.Clamp(posPx.X, leftPx, rightPx);
                return posPx / UIScale;
            }

            return base.ClampSelectionPosition(relativePosition);
        }

        protected internal override void KeyboardFocusExited()
        {
            base.KeyboardFocusExited();
            ClearSelectionState();
        }

        protected override void Resized()
        {
            base.Resized();

            var styleBoxSize = _getStyleBox()?.MinimumSize.Y ?? 0;

            _scrollBar.Page = UIScale * (Height - styleBoxSize);
            _updateScrollButtonVisibility();
            _invalidateEntries();
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            return _getStyleBox()?.MinimumSize ?? Vector2.Zero;
        }

        internal void _invalidateEntries()
        {
            _totalContentHeight = 0;
            var font = _getFont();
            var sizeX = _getContentBox().Width;
            foreach (ref var entry in _entries)
            {
                entry.Update(_tagManager, font, sizeX, UIScale);
                _totalContentHeight += entry.Height + font.GetLineSeparation(UIScale);
            }

            _scrollBar.MaxValue = Math.Max(_scrollBar.Page, _totalContentHeight);
            if (_isAtBottom && ScrollFollowing)
            {
                _scrollBar.MoveToEnd();
            }
        }

        [Pure]
        private Font _getFont()
        {
            if (TryGetStyleProperty<Font>("font", out var font))
            {
                return font;
            }

            return UserInterfaceManager.ThemeDefaults.DefaultFont;
        }

        [Pure]
        private StyleBox? _getStyleBox()
        {
            if (StyleBoxOverride != null)
            {
                return StyleBoxOverride;
            }

            TryGetStyleProperty<StyleBox>(StylePropertyStyleBox, out var box);
            return box;
        }

        [Pure]
        private float _getScrollSpeed()
        {
            // The scroll speed depends on the UI scale because the scroll bar is working with physical pixels.
            return GetScrollSpeed(_getFont(), UIScale);
        }

        [Pure]
        private UIBox2 _getContentBox()
        {
            var style = _getStyleBox();
            var box = style?.GetContentBox(PixelSizeBox, UIScale) ?? PixelSizeBox;
            box.Right = Math.Max(box.Left, box.Right - _scrollBar.DesiredPixelSize.X);
            return box;
        }

        private bool TryGetEntryAtPosition(Vector2 relativePosition, out int entryIndex, out float entryOffset)
        {
            entryIndex = -1;
            entryOffset = 0;

            if (_entries.Count == 0)
                return false;

            var font = _getFont();
            var lineSeparation = font.GetLineSeparation(UIScale);
            var contentBox = _getContentBox();
            var yPx = relativePosition.Y * UIScale;

            var offset = -_scrollBar.Value;
            for (var i = 0; i < _entries.Count; i++)
            {
                var localEntry = _entries[i];
                var top = contentBox.Top + offset;
                var bottom = top + localEntry.Height;
                if (yPx >= top && yPx <= bottom)
                {
                    entryIndex = i;
                    entryOffset = offset;
                    return true;
                }

                offset += localEntry.Height + lineSeparation;
            }

            return false;
        }

        /// <summary>
        ///     Calculates the vertical offset for an entry using current scroll position.
        /// </summary>
        private float GetEntryOffset(int entryIndex)
        {
            var font = _getFont();
            var lineSeparation = font.GetLineSeparation(UIScale);
            var offset = -_scrollBar.Value;

            for (var i = 0; i < entryIndex && i < _entries.Count; i++)
            {
                offset += _entries[i].Height + lineSeparation;
            }

            return offset;
        }

        private int GetIndexAtEntry(int entryIndex, float entryOffset, Vector2 relativePosition)
        {
            var entry = _entries[entryIndex];
            return entry.GetIndexAtPosition(
                _tagManager,
                _getFont(),
                _getContentBox(),
                entryOffset,
                relativePosition * UIScale,
                UIScale,
                1);
        }

        private void ClearSelectionState()
        {
            _selectedEntryIndex = null;
            ClearSelection();
        }

        protected internal override void UIScaleChanged()
        {
            // If this control isn't visible, don't invalidate entries immediately.
            // This saves invalidating the debug console if it's hidden,
            // which is a huge boon as auto-scaling changes UI scale a lot in that scenario.
            if (!VisibleInTree)
                _invalidOnVisible = true;
            else
                _invalidateEntries();

            base.UIScaleChanged();
        }

        protected override void StylePropertiesChanged()
        {
            base.StylePropertiesChanged();

            // Font may have changed.
            _invalidateEntries();
        }

        internal static float GetScrollSpeed(Font font, float scale)
        {
            return font.GetLineHeight(scale) * 2;
        }

        protected override void EnteredTree()
        {
            base.EnteredTree();
            // Due to any number of reasons the entries may be invalidated if added when not visible in the tree.
            // e.g. the control has not had its UI scale set and the messages were added, but the
            // existing ones were valid when the UI scale was set.
            _invalidateEntries();
        }

        protected override void VisibilityChanged(bool newVisible)
        {
            if (newVisible && _invalidOnVisible)
            {
                _invalidateEntries();
                _invalidOnVisible = false;
            }
        }

        private void _updateScrollButtonVisibility()
        {
            _scrollDownButton.Visible = ShowScrollDownButton && !_isAtBottom;
        }

        private sealed class OutputPanelSelectionLayout(OutputPanel owner) : ISelectableTextLayout
        {
            private readonly OutputPanel _owner = owner;

            public ReadOnlySpan<char> GetTextSpan()
            {
                if (_owner._entries.Count == 0)
                    return [];

                if (_owner._selectedEntryIndex is { } entryIndex && entryIndex < _owner._entries.Count)
                    return _owner._entries[entryIndex].GetPlainText(_owner._tagManager, _owner._getFont()).AsSpan();

                // If there's no active selection/entry picked, default to copying all visible output.
                // This matches the behavior of other display-only copyable controls (e.g. Label).
                var builder = new System.Text.StringBuilder();
                for (var i = 0; i < _owner._entries.Count; i++)
                {
                    if (i != 0)
                        builder.Append('\n');

                    builder.Append(_owner._entries[i].GetPlainText(_owner._tagManager, _owner._getFont()));
                }

                _owner._copyCache = builder.ToString();
                return _owner._copyCache.AsSpan();
            }

            public int GetIndexAtPosition(Vector2 relativePosition)
            {
                if (_owner._entries.Count == 0)
                    return 0;

                if (_owner.IsSelecting && _owner._selectedEntryIndex is { } lockedIndex && lockedIndex < _owner._entries.Count)
                {
                    var entryOffset = _owner.GetEntryOffset(lockedIndex);
                    return _owner.GetIndexAtEntry(lockedIndex, entryOffset, relativePosition);
                }

                if (!_owner.TryGetEntryAtPosition(relativePosition, out var entryIndex, out var entryOffsetPosition))
                    return 0;

                _owner._selectedEntryIndex = entryIndex;

                return _owner.GetIndexAtEntry(entryIndex, entryOffsetPosition, relativePosition);
            }

            public void DrawSelection(DrawingHandleScreen handle, int selectionLower, int selectionUpper, Color color)
            {
                if (_owner._selectedEntryIndex is not { } entryIndex || entryIndex >= _owner._entries.Count)
                    return;

                var entry = _owner._entries[entryIndex];
                var entryOffset = _owner.GetEntryOffset(entryIndex);
                entry.DrawSelection(_owner._tagManager, handle, _owner._getFont(), _owner._getContentBox(), entryOffset,
                    new MarkupDrawingContext(), _owner.UIScale, 1, selectionLower, selectionUpper, color);
            }
        }
    }
}
