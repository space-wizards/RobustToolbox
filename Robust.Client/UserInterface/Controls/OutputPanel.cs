using System;
using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     A control to handle output of message-by-message output panels, like the debug console and chat panel.
    /// </summary>
    public class OutputPanel : Control
    {
        public const string StylePropertyStyleBox = "stylebox";

        private readonly List<RichTextEntry> _entries = new();
        private bool _isAtBottom = true;

        private int _totalContentHeight;
        private bool _firstLine = true;
        private StyleBox? _styleBoxOverride;
        private VScrollBar _scrollBar;

        public bool ScrollFollowing { get; set; } = true;

        public OutputPanel()
        {
            MouseFilter = MouseFilterMode.Pass;
            RectClipContent = true;

            _scrollBar = new VScrollBar
            {
                Name = "_v_scroll",
                HorizontalAlignment = HAlignment.Right
            };
            AddChild(_scrollBar);
            _scrollBar.OnValueChanged += _ => _isAtBottom = _scrollBar.IsAtEnd;
        }

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
            _invalidateEntries();
        }

        public void RemoveEntry(Index index)
        {
            var entry = _entries[index];
            _entries.RemoveAt(index.GetOffset(_entries.Count));

            if (_entries.Count == 0)
                Clear();

            _invalidateEntries();
        }

        public void AddText(string text)
        {
            var msg = new FormattedMessage.Builder();
            msg.AddText(text);
            AddMessage(msg.Build());
        }

        public void AddMessage(FormattedMessage message)
        {
            var entry = new RichTextEntry(message);

            _entries.Add(entry);

            if (_firstLine)
                _firstLine = false;

            _invalidateEntries();
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
            var flib = _getFontLib();
            var font = _getFont();
            style?.Draw(handle, PixelSizeBox);
            var contentBox = _getContentBox();

            var entryOffset = -_scrollBar.Value;

            foreach (var entry in _entries)
            {
                if (entryOffset + entry.Height < 0)
                {
                    entryOffset += entry.Height;
                    continue;
                }

                if (entryOffset > contentBox.Height)
                {
                    break;
                }

                entry.Draw(handle, flib, contentBox, entryOffset, UIScale, _getFontColor());

                entryOffset += entry.Height;
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

            _scrollBar.Page = PixelSize.Y - styleBoxSize;
            _invalidateEntries();
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            return _getStyleBox()?.MinimumSize ?? Vector2.Zero;
        }

        private void _invalidateEntries()
        {
            _totalContentHeight = 0;
            var font = _getFontLib();
            var sizeX = _getContentBox().Width;
            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                entry.Update(font, sizeX, UIScale);
                _entries[i] = entry;
                _totalContentHeight += entry.Height;
            }

            _scrollBar.MaxValue = Math.Max(_scrollBar.Page, _totalContentHeight);
            if (_isAtBottom && ScrollFollowing)
            {
                _scrollBar.MoveToEnd();
            }
        }

        [System.Diagnostics.Contracts.Pure]
        private IFontLibrary _getFontLib()
        {
            if (TryGetStyleProperty<IFontLibrary>("font-library", out var flib))
                return flib;

            return UserInterfaceManager
                .ThemeDefaults
                .DefaultFontLibrary;
        }

        [System.Diagnostics.Contracts.Pure]
        private Font _getFont()
        {
            TryGetStyleProperty<FontClass>("font", out var fclass);
            return _getFontLib().StartFont(fclass).Current;
        }

        [System.Diagnostics.Contracts.Pure]
        private Color _getFontColor()
        {
            if (TryGetStyleProperty<Color>("font-color", out var fc))
                return fc;

            // From Robust.Client/UserInterface/RichTextEntry.cs#L19
            // at 33008a2bce0cc4755b18b12edfaf5b6f1f87fdd9
            return new Color(200, 200, 200);
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
        private int _getScrollSpeed()
        {
            return _getFont().GetLineHeight(UIScale) * 2;
        }

        [System.Diagnostics.Contracts.Pure]
        private UIBox2 _getContentBox()
        {
            var style = _getStyleBox();
            return style?.GetContentBox(PixelSizeBox) ?? PixelSizeBox;
        }

        protected internal override void UIScaleChanged()
        {
            _invalidateEntries();

            base.UIScaleChanged();
        }
    }
}
