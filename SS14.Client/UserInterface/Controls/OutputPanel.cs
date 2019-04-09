using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Drawing;
using SS14.Client.Input;
using SS14.Client.Utility;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using PureAttribute = System.Diagnostics.Contracts.PureAttribute;

namespace SS14.Client.UserInterface.Controls
{
    /// <summary>
    ///     A control to handle output of message-by-message output panels, like the debug console and chat panel.
    /// </summary>
    public class OutputPanel : Control
    {
        public const string StylePropertyStyleBox = "stylebox";

        private readonly List<RichTextEntry> _entries = new List<RichTextEntry>();
        private bool _isAtBottom = true;

        // These two are used to implement this on Godot.
        private PanelContainer _godotPanelContainer;
        private Godot.RichTextLabel _godotRichTextLabel;
        private int _totalContentHeight;
        private bool _firstLine = true;
        private StyleBox _styleBoxOverride;
        private VScrollBar _scrollBar;

        public bool ScrollFollowing { get; set; } = true;

        private int ScrollLimit => Math.Max(0, _totalContentHeight - (int) _getContentBox().Height + 1);

        public StyleBox StyleBoxOverride
        {
            get => _styleBoxOverride;
            set
            {
                _styleBoxOverride = value;
                if (GameController.OnGodot)
                {
                    // Have to set this to empty so Godot doesn't set it to that ugly default one.
                    _godotPanelContainer.PanelOverride = value ?? new StyleBoxEmpty();
                }
                else
                {
                    MinimumSizeChanged();
                    _invalidateEntries();
                }
            }
        }

        public void Clear()
        {
            _firstLine = true;
            if (GameController.OnGodot)
            {
                _godotRichTextLabel.Clear();
            }
            else
            {
                _entries.Clear();
                _totalContentHeight = 0;
                _scrollBar.MaxValue = Math.Max(_scrollBar.Page, _totalContentHeight);
                _scrollBar.Value = 0;
            }
        }

        public void RemoveLine(int line)
        {
            if (GameController.OnGodot)
            {
                _godotRichTextLabel.RemoveLine(line);
                return;
            }

            var entry = _entries[line];
            _entries.RemoveAt(line);

            var font = _getFont();
            _totalContentHeight -= entry.Height + font.LineSeparation;
            if (_entries.Count == 0)
            {
                Clear();
            }
            _scrollBar.MaxValue = Math.Max(_scrollBar.Page, _totalContentHeight);
        }

        public void AddMessage(FormattedMessage message)
        {
            if (GameController.OnGodot)
            {
                _addMessageGodot(message);
                return;
            }

            var entry = new RichTextEntry(message);

            entry.Update(_getFont(), _getContentBox().Width);

            _entries.Add(entry);
            var font = _getFont();
            _totalContentHeight += entry.Height;
            if (_firstLine)
            {
                _firstLine = false;
            }
            else
            {
                _totalContentHeight += font.LineSeparation;
            }

            _scrollBar.MaxValue = Math.Max(_scrollBar.Page, _totalContentHeight);
            if (_isAtBottom && ScrollFollowing)
            {
                _scrollBar.Value = ScrollLimit;
            }
        }

        public void ScrollToBottom()
        {
            _scrollBar.Value = ScrollLimit;
            _isAtBottom = true;
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (GameController.OnGodot)
            {
                _godotPanelContainer = new PanelContainer {PanelOverride = new StyleBoxEmpty()};
                _godotPanelContainer.SetAnchorPreset(LayoutPreset.Wide);
                AddChild(_godotPanelContainer);
                _godotRichTextLabel = new Godot.RichTextLabel {ScrollFollowing = true};
                _godotPanelContainer.SceneControl.AddChild(_godotRichTextLabel);
            }
            else
            {
                _scrollBar = new VScrollBar {Name = "_v_scroll"};
                AddChild(_scrollBar);
                _scrollBar.SetAnchorAndMarginPreset(LayoutPreset.RightWide);
                _scrollBar.OnValueChanged += _ => _isAtBottom = _scrollBar.IsAtEnd;
            }
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            if (GameController.OnGodot)
            {
                return;
            }

            var style = _getStyleBox();
            var font = _getFont();
            style?.Draw(handle, SizeBox);
            var contentBox = _getContentBox();

            var entryOffset = -_scrollBar.Value;

            // A stack for format tags.
            // This stack contains the format tag to RETURN TO when popped off.
            // So when a new color tag gets hit this stack gets the previous color pushed on.
            var formatStack = new Stack<FormattedMessage.Tag>(2);

            foreach (var entry in _entries)
            {
                if (entryOffset + entry.Height < 0)
                {
                    entryOffset += entry.Height + font.LineSeparation;
                    continue;
                }

                if (entryOffset > contentBox.Height)
                {
                    break;
                }

                entry.Draw(handle, font, contentBox, entryOffset, formatStack);

                entryOffset += entry.Height + font.LineSeparation;
            }
        }

        protected internal override void MouseWheel(GUIMouseWheelEventArgs args)
        {
            base.MouseWheel(args);

            if (GameController.OnGodot)
            {
                return;
            }

            if (args.WheelDirection == Mouse.Wheel.Up)
            {
                _scrollBar.Value = _scrollBar.Value - _getScrollSpeed();
                _isAtBottom = false;
            }
            else if (args.WheelDirection == Mouse.Wheel.Down)
            {
                var limit = ScrollLimit;
                _scrollBar.Value = _scrollBar.Value + _getScrollSpeed();
                if (_scrollBar.IsAtEnd)
                {
                    _isAtBottom = true;
                }
            }
        }

        private void _addMessageGodot(FormattedMessage message)
        {
            DebugTools.Assert(GameController.OnGodot);

            if (!_firstLine)
            {
                _godotRichTextLabel.Newline();
            }
            else
            {
                _firstLine = false;
            }

            var pushCount = 0;
            foreach (var tag in message.Tags)
                switch (tag)
                {
                    case FormattedMessage.TagText text:
                        _godotRichTextLabel.AddText(text.Text);
                        break;
                    case FormattedMessage.TagColor color:
                        _godotRichTextLabel.PushColor(color.Color.Convert());
                        pushCount++;
                        break;
                    case FormattedMessage.TagPop _:
                        if (pushCount <= 0) throw new InvalidOperationException();

                        _godotRichTextLabel.Pop();
                        pushCount--;
                        break;
                }

            for (; pushCount > 0; pushCount--)
            {
                _godotRichTextLabel.Pop();
            }
        }

        protected override void SetDefaults()
        {
            base.SetDefaults();

            RectClipContent = true;
        }

        protected override void Resized()
        {
            base.Resized();

            if (GameController.OnGodot)
            {
                return;
            }

            var styleBoxSize = _getStyleBox()?.MinimumSize.Y ?? 0;

            _scrollBar.Page = Size.Y - styleBoxSize;
            _invalidateEntries();
        }

        protected override Vector2 CalculateMinimumSize()
        {
            return _getStyleBox()?.MinimumSize ?? Vector2.Zero;
        }

        private void _invalidateEntries()
        {
            _totalContentHeight = 0;
            var font = _getFont();
            var sizeX = _getContentBox().Width;
            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                entry.Update(font, sizeX);
                _entries[i] = entry;
                _totalContentHeight += entry.Height + font.LineSeparation;
            }

            _scrollBar.MaxValue = Math.Max(_scrollBar.Page, _totalContentHeight);
            if (_isAtBottom && ScrollFollowing)
            {
                _scrollBar.Value = ScrollLimit;
            }
        }

        [Pure]
        private Font _getFont()
        {
            if (TryGetStyleProperty("font", out Font font))
            {
                return font;
            }

            return UserInterfaceManager.ThemeDefaults.DefaultFont;
        }

        [Pure]
        [CanBeNull]
        private StyleBox _getStyleBox()
        {
            if (StyleBoxOverride != null)
            {
                return StyleBoxOverride;
            }

            TryGetStyleProperty(StylePropertyStyleBox, out StyleBox box);
            return box;
        }

        [Pure]
        private int _getScrollSpeed()
        {
            var font = _getFont();
            return font.Height * 2;
        }

        [Pure]
        private UIBox2 _getContentBox()
        {
            var style = _getStyleBox();
            return style?.GetContentBox(SizeBox) ?? SizeBox;
        }
    }
}
