using System;
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
    public class OutputPanel : Control
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

        public bool ScrollFollowing { get; set; } = true;

        private bool _invalidOnVisible;

        public OutputPanel()
        {
            IoCManager.InjectDependencies(this);
            MouseFilter = MouseFilterMode.Pass;
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
            _entries.Clear();
            _totalContentHeight = 0;
            _scrollBar.MaxValue = Math.Max(_scrollBar.Page, _totalContentHeight);
            _scrollBar.Value = 0;
        }

        public void RemoveEntry(Index index)
        {
            var entry = _entries[index];
            _entries.RemoveAt(index.GetOffset(_entries.Count));

            var font = _getFont();
            _totalContentHeight -= entry.Height + font.GetLineSeparation(UIScale);
            if (_entries.Count == 0)
            {
                Clear();
            }

            _scrollBar.MaxValue = Math.Max(_scrollBar.Page, _totalContentHeight);
        }

        public void AddText(string text)
        {
            var msg = new FormattedMessage();
            msg.AddText(text);
            AddMessage(msg);
        }

        public void AddMessage(FormattedMessage message)
        {
            var entry = new RichTextEntry(message, this, _tagManager, null);

            entry.Update(_tagManager, _getFont(), _getContentBox().Width, UIScale);

            _entries.Add(entry);
            var font = _getFont();
            _totalContentHeight += entry.Height;
            if (_firstLine)
            {
                _firstLine = false;
            }
            else
            {
                _totalContentHeight += font.GetLineSeparation(UIScale);
            }

            _scrollBar.MaxValue = Math.Max(_scrollBar.Page, _totalContentHeight);
            if (_isAtBottom && ScrollFollowing)
            {
                _scrollBar.MoveToEnd();
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
                    continue;
                }

                if (entryOffset > contentBox.Height)
                {
                    entry.HideControls();
                    continue;
                }

                entry.Draw(_tagManager, handle, font, contentBox, entryOffset, context, UIScale);

                entryOffset += entry.Height + lineSeparation;
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

        private void _invalidateEntries()
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

        [System.Diagnostics.Contracts.Pure]
        private Font _getFont()
        {
            if (TryGetStyleProperty<Font>("font", out var font))
            {
                return font;
            }

            return UserInterfaceManager.ThemeDefaults.DefaultFont;
        }

        [System.Diagnostics.Contracts.Pure]
        private StyleBox? _getStyleBox()
        {
            if (StyleBoxOverride != null)
            {
                return StyleBoxOverride;
            }

            TryGetStyleProperty<StyleBox>(StylePropertyStyleBox, out var box);
            return box;
        }

        [System.Diagnostics.Contracts.Pure]
        private float _getScrollSpeed()
        {
            // The scroll speed depends on the UI scale because the scroll bar is working with physical pixels.
            return GetScrollSpeed(_getFont(), UIScale);
        }

        [System.Diagnostics.Contracts.Pure]
        private UIBox2 _getContentBox()
        {
            var style = _getStyleBox();
            var box = style?.GetContentBox(PixelSizeBox, UIScale) ?? PixelSizeBox;
            box.Right = Math.Max(box.Left, box.Right - _scrollBar.DesiredPixelSize.X);
            return box;
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
    }
}
