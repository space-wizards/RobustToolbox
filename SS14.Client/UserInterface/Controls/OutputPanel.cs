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

        private static readonly FormattedMessage.TagColor TagWhite = new FormattedMessage.TagColor(Color.White);
        private readonly List<Entry> _entries = new List<Entry>();
        private bool _isAtBottom = true;
        private int _mouseWheelOffset;
        // These two are used to implement this on Godot.
        private PanelContainer _godotPanelContainer;
        private RichTextLabel _godotRichTextLabel;
        private int _totalContentHeight;
        private bool rtlFirstLine = true;
        private StyleBox _styleBoxOverride;

        public bool ScrollFollowing { get; set; } = true;

        private int ScrollLimit => Math.Max(0, _totalContentHeight - (int) Size.Y + 1);

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
                    _invalidateEntries();
                    MinimumSizeChanged();
                }
            }
        }

        public void Clear()
        {
            if (GameController.OnGodot)
            {
                _godotRichTextLabel.Clear();
                rtlFirstLine = true;
            }
            else
            {
                _entries.Clear();
                _totalContentHeight = 0;
                _mouseWheelOffset = 0;
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
        }

        public void AddMessage(FormattedMessage message)
        {
            if (GameController.OnGodot)
            {
                _addMessageGodot(message);
                return;
            }

            var entry = new Entry(message);

            _updateEntry(ref entry);

            _entries.Add(entry);
            var font = _getFont();
            _totalContentHeight += font.LineSeparation + entry.Height;
            if (_isAtBottom && ScrollFollowing)
            {
                _mouseWheelOffset = ScrollLimit;
            }
        }

        public void ScrollToBottom()
        {
            _mouseWheelOffset = ScrollLimit;
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
                _godotRichTextLabel = new RichTextLabel {ScrollFollowing = true};
                _godotPanelContainer.AddChild(_godotRichTextLabel);
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
            var contentBox = style?.GetContentBox(SizeBox) ?? SizeBox;
            style?.Draw(handle, SizeBox);

            var entryOffset = 0;

            // A stack for format tags.
            // This stack contains the format tag to RETURN TO when popped off.
            // So when a new color tag gets hit this stack gets the previous color pushed on.
            var formatStack = new Stack<FormattedMessage.Tag>(2);

            foreach (var entry in _entries)
            {
                if (entryOffset - _mouseWheelOffset < 0)
                {
                    entryOffset += entry.Height + font.LineSeparation;
                    continue;
                }

                if (entryOffset + entry.Height - _mouseWheelOffset > contentBox.Height)
                {
                    break;
                }

                // The tag currently doing color.
                var currentColorTag = TagWhite;

                var globalBreakCounter = 0;
                var lineBreakIndex = 0;
                var baseLine = contentBox.TopLeft + new Vector2(0, font.Ascent + entryOffset - _mouseWheelOffset);
                formatStack.Clear();
                foreach (var tag in entry.Message.Tags)
                {
                    switch (tag)
                    {
                        case FormattedMessage.TagColor tagColor:
                            formatStack.Push(currentColorTag);
                            currentColorTag = tagColor;
                            break;
                        case FormattedMessage.TagPop _:
                            var popped = formatStack.Pop();
                            switch (popped)
                            {
                                case FormattedMessage.TagColor tagColor:
                                    currentColorTag = tagColor;
                                    break;
                                default:
                                    throw new InvalidOperationException();
                            }

                            break;
                        case FormattedMessage.TagText tagText:
                        {
                            var text = tagText.Text;
                            for (var i = 0; i < text.Length; i++, globalBreakCounter++)
                            {
                                var chr = text[i];
                                if (lineBreakIndex < entry.LineBreaks.Count &&
                                    entry.LineBreaks[lineBreakIndex] == globalBreakCounter)
                                {
                                    baseLine = new Vector2(contentBox.Left, baseLine.Y + font.LineHeight);
                                    lineBreakIndex += 1;
                                }

                                var advance = font.DrawChar(handle, chr, baseLine, currentColorTag.Color);
                                baseLine += new Vector2(advance, 0);
                            }

                            break;
                        }
                    }
                }

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
                _mouseWheelOffset = Math.Max(0, _mouseWheelOffset - _getScrollSpeed());
                _isAtBottom = false;
            }
            else if (args.WheelDirection == Mouse.Wheel.Down)
            {
                var limit = ScrollLimit;
                _mouseWheelOffset = Math.Min(_mouseWheelOffset + _getScrollSpeed(), limit);
                if (limit == _mouseWheelOffset)
                {
                    _isAtBottom = true;
                }
            }
        }

        private void _addMessageGodot(FormattedMessage message)
        {
            DebugTools.Assert(GameController.OnGodot);

            if (!rtlFirstLine)
            {
                _godotRichTextLabel.NewLine();
            }
            else
            {
                rtlFirstLine = false;
            }

            var pushCount = 0;
            foreach (var tag in message.Tags)
                switch (tag)
                {
                    case FormattedMessage.TagText text:
                        _godotRichTextLabel.AddText(text.Text);
                        break;
                    case FormattedMessage.TagColor color:
                        _godotRichTextLabel.PushColor(color.Color);
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

        /// <summary>
        ///     Recalculate line dimensions and where it has line breaks for word wrapping.
        /// </summary>
        private void _updateEntry(ref Entry entry)
        {
            DebugTools.Assert(!GameController.OnGodot);
            // This method is gonna suck due to complexity.
            // Bear with me here.
            // I am so deeply sorry for the person adding stuff to this in the future.
            var font = _getFont();
            var contentBox = _getStyleBox()?.GetContentBox(SizeBox) ?? SizeBox;
            // Horizontal size we have to work with here.
            var sizeX = contentBox.Width;
            entry.Height = font.Height;
            entry.LineBreaks.Clear();

            // Index we put into the LineBreaks list when a line break should occur.
            var breakIndexCounter = 0;
            // If the CURRENT processing word ends up too long, this is the index to put a line break.
            int? wordStartBreakIndex = null;
            // Word size in pixels.
            var wordSizePixels = 0;
            // The horizontal position of the text cursor.
            var posX = 0;
            var lastChar = 'A';
            // If a word is larger than sizeX, we split it.
            // We need to keep track of some data to split it into two words.
            (int breakIndex, int wordSizePixels)? forceSplitData = null;
            // Go over every text tag.
            // We treat multiple text tags as one continuous one.
            // So changing color inside a single word doesn't create a word break boundary.
            foreach (var tag in entry.Message.Tags)
            {
                if (!(tag is FormattedMessage.TagText tagText))
                {
                    continue;
                }

                var text = tagText.Text;
                // And go over every character.
                for (var i = 0; i < text.Length; i++, breakIndexCounter++)
                {
                    var chr = text[i];

                    if (IsWordBoundary(lastChar, chr) || chr == '\n')
                    {
                        // Word boundary means we know where the word ends.
                        if (posX > sizeX)
                        {
                            DebugTools.Assert(wordStartBreakIndex.HasValue,
                                "wordStartBreakIndex can only be null if the word begins at a new line, in which case this branch shouldn't be reached as the word would be split due to being longer than a single line.");
                            // We ran into a word boundary and the word is too big to fit the previous line.
                            // So we insert the line break BEFORE the last word.
                            entry.LineBreaks.Add(wordStartBreakIndex.Value);
                            entry.Height += font.LineHeight;
                            posX = wordSizePixels;
                        }

                        // Start a new word since we hit a word boundary.
                        //wordSize = 0;
                        wordSizePixels = 0;
                        wordStartBreakIndex = breakIndexCounter;
                        forceSplitData = null;

                        // Just manually handle newlines.
                        if (chr == '\n')
                        {
                            entry.LineBreaks.Add(breakIndexCounter);
                            entry.Height += font.LineHeight;
                            posX = 0;
                            lastChar = chr;
                            wordStartBreakIndex = null;
                            continue;
                        }
                    }

                    // Uh just skip unknown characters I guess.
                    if (!font.TryGetCharMetrics(chr, out var metrics))
                    {
                        lastChar = chr;
                        continue;
                    }

                    // Increase word size and such with the current character.
                    var oldWordSizePixels = wordSizePixels;
                    wordSizePixels += metrics.Advance;
                    // TODO: Theoretically, does it make sense to break after the glyph's width instead of its advance?
                    //   It might result in some more tight packing but I doubt it'd be noticeable.
                    //   Also definitely even more complex to implement.
                    posX += metrics.Advance;

                    if (posX > sizeX)
                    {
                        if (!forceSplitData.HasValue)
                        {
                            forceSplitData = (breakIndexCounter, oldWordSizePixels);
                        }

                        // Oh hey we get to break a word that doesn't fit on a single line.
                        if (wordSizePixels > sizeX)
                        {
                            var (breakIndex, splitWordSize) = forceSplitData.Value;
                            if (splitWordSize == 0) return;

                            // Reset forceSplitData so that we can split again if necessary.
                            forceSplitData = null;
                            entry.LineBreaks.Add(breakIndex);
                            entry.Height += font.LineHeight;
                            wordSizePixels -= splitWordSize;
                            wordStartBreakIndex = null;
                            posX = wordSizePixels;
                        }
                    }

                    lastChar = chr;
                }
            }

            // This needs to happen because word wrapping doesn't get checked for the last word.
            if (posX > sizeX)
            {
                DebugTools.Assert(wordStartBreakIndex.HasValue,
                    "wordStartBreakIndex can only be null if the word begins at a new line, in which case this branch shouldn't be reached as the word would be split due to being longer than a single line.");
                entry.LineBreaks.Add(wordStartBreakIndex.Value);
                entry.Height += font.LineHeight;
            }
        }

        protected override void Resized()
        {
            base.Resized();

            if (GameController.OnGodot)
            {
                return;
            }

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
            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                _updateEntry(ref entry);
                _entries[i] = entry;
                _totalContentHeight += entry.Height + font.LineSeparation;
            }

            if (_isAtBottom && ScrollFollowing)
            {
                _mouseWheelOffset = ScrollLimit;
            }
        }

        [Pure]
        private static bool IsWordBoundary(char a, char b)
        {
            return a == ' ' || b == ' ' || a == '-' || b == '-';
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

        private struct Entry
        {
            public readonly FormattedMessage Message;

            /// <summary>
            ///     The size of this line, in pixels.
            /// </summary>
            public int Height;

            /// <summary>
            ///     The combined text indices in the message's text tags to put line breaks.
            /// </summary>
            public readonly List<int> LineBreaks;

            public Entry(FormattedMessage message)
            {
                Message = message;
                Height = 0;
                LineBreaks = new List<int>();
            }
        }
    }
}
