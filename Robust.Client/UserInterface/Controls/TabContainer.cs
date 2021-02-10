using System;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Shared.Input;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    public class TabContainer : Container
    {
        public static readonly AttachedProperty<bool> TabVisibleProperty = AttachedProperty<bool>.Create("TabVisible", typeof(TabContainer), true);
        public static readonly AttachedProperty<string?> TabTitleProperty = AttachedProperty<string?>.CreateNull("TabTitle", typeof(TabContainer));

        public const string StylePropertyTabStyleBox = "tab-stylebox";
        public const string StylePropertyTabStyleBoxInactive = "tab-stylebox-inactive";
        public const string stylePropertyTabFontColor = "tab-font-color";
        public const string StylePropertyTabFontColorInactive = "tab-font-color-inactive";
        public const string StylePropertyPanelStyleBox = "panel-stylebox";

        private int _currentTab;
        private bool _tabsVisible = true;

        public int CurrentTab
        {
            get => _currentTab;
            set
            {
                if (_currentTab < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Current tab must be positive.");
                }

                if (_currentTab >= ChildCount)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        "Current tab must less than the amount of tabs.");
                }

                if (_currentTab == value)
                {
                    return;
                }

                var old = _currentTab;
                _currentTab = value;

                GetChild(old).Visible = false;
                var newSelected = GetChild(value);
                newSelected.Visible = true;
                _fixChildMargins(newSelected);

                OnTabChanged?.Invoke(value);
            }
        }

        public bool TabsVisible
        {
            get => _tabsVisible;
            set
            {
                _tabsVisible = value;
                MinimumSizeChanged();
            }
        }

        public event Action<int>? OnTabChanged;

        public TabContainer()
        {
            MouseFilter = MouseFilterMode.Pass;
        }

        public string GetActualTabTitle(int tab)
        {
            var control = GetChild(tab);
            var title = control.GetValue(TabTitleProperty);

            return title ?? control.Name ?? Loc.GetString("No title");
        }

        public static string? GetTabTitle(Control control)
        {
            return control.GetValue(TabTitleProperty);
        }

        public bool GetTabVisible(int tab)
        {
            var control = GetChild(tab);
            return GetTabVisible(control);
        }

        public static bool GetTabVisible(Control control)
        {
            return control.GetValue(TabVisibleProperty);
        }

        public void SetTabTitle(int tab, string title)
        {
            var control = GetChild(tab);
            SetTabTitle(control, title);
        }

        public static void SetTabTitle(Control control, string title)
        {
            control.SetValue(TabTitleProperty, title);
        }

        public void SetTabVisible(int tab, bool visible)
        {
            var control = GetChild(tab);
            SetTabVisible(control, visible);
        }

        public static void SetTabVisible(Control control, bool visible)
        {
            control.SetValue(TabVisibleProperty, visible);
        }

        protected override void ChildAdded(Control newChild)
        {
            base.ChildAdded(newChild);

            if (ChildCount == 1)
            {
                // This is our first child so it must always be visible.
                newChild.Visible = true;
                _fixChildMargins(newChild);
            }
            else
            {
                // If not this can't be the currently selected tab so just make it invisible immediately.
                newChild.Visible = false;
            }
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            // First, draw panel.
            var headerSize = _getHeaderSize();
            var panel = _getPanel();
            var panelBox = new UIBox2(0, headerSize, PixelWidth, PixelHeight);

            panel?.Draw(handle, panelBox);

            var font = _getFont();
            var boxActive = _getTabBoxActive();
            var boxInactive = _getTabBoxInactive();
            var fontColorActive = _getTabFontColorActive();
            var fontColorInactive = _getTabFontColorInactive();

            var headerOffset = 0f;

            // Then, draw the tabs.
            for (var i = 0; i < ChildCount; i++)
            {
                if (!GetTabVisible(i))
                {
                    continue;
                }

                var title = GetActualTabTitle(i);

                var titleLength = 0;
                // Get string length.
                foreach (var chr in title)
                {
                    if (!font.TryGetCharMetrics(chr, UIScale, out var metrics))
                    {
                        continue;
                    }

                    titleLength += metrics.Advance;
                }

                var active = _currentTab == i;
                var box = active ? boxActive : boxInactive;

                UIBox2 contentBox;
                var topLeft = new Vector2(headerOffset, 0);
                var size = new Vector2(titleLength, font.GetHeight(UIScale));
                float boxAdvance;

                if (box != null)
                {
                    var drawBox = box.GetEnvelopBox(topLeft, size);
                    boxAdvance = drawBox.Width;
                    box.Draw(handle, drawBox);
                    contentBox = box.GetContentBox(drawBox);
                }
                else
                {
                    boxAdvance = size.X;
                    contentBox = UIBox2.FromDimensions(topLeft, size);
                }

                var baseLine = new Vector2(0, font.GetAscent(UIScale)) + contentBox.TopLeft;

                foreach (var chr in title)
                {
                    if (!font.TryGetCharMetrics(chr, UIScale, out var metrics))
                    {
                        continue;
                    }

                    font.DrawChar(handle, chr, baseLine, UIScale, active ? fontColorActive : fontColorInactive);
                    baseLine += new Vector2(metrics.Advance, 0);
                }

                headerOffset += boxAdvance;
            }
        }

        protected override Vector2 CalculateMinimumSize()
        {
            var total = Vector2i.Zero;

            foreach (var child in Children)
            {
                if (child.Visible)
                {
                    total = Vector2i.ComponentMax(child.CombinedPixelMinimumSize, total);
                }
            }

            if (TabsVisible)
            {
                total += (0, _getHeaderSize());
            }

            var panel = _getPanel();
            total += (Vector2i)(panel?.MinimumSize ?? Vector2.Zero);

            return total / UIScale;
        }

        private void _fixChildMargins(Control child)
        {
            FitChildInPixelBox(child, _getContentBox());
        }

        protected override void LayoutUpdateOverride()
        {
            if (ChildCount == 0 || _currentTab >= ChildCount)
            {
                return;
            }

            var control = GetChild(_currentTab);
            control.Visible = true;
            _fixChildMargins(control);
        }

        protected internal override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            base.KeyBindDown(args);

            if (!TabsVisible || args.Function != EngineKeyFunctions.UIClick)
            {
                return;
            }

            // Outside of header size, ignore.
            if (args.RelativePixelPosition.Y < 0 || args.RelativePixelPosition.Y > _getHeaderSize())
            {
                return;
            }

            args.Handle();

            var relX = args.RelativePixelPosition.X;

            var font = _getFont();
            var boxActive = _getTabBoxActive();
            var boxInactive = _getTabBoxInactive();

            var headerOffset = 0f;

            for (var i = 0; i < ChildCount; i++)
            {
                if (!GetTabVisible(i))
                {
                    continue;
                }

                var title = GetActualTabTitle(i);

                var titleLength = 0;
                // Get string length.
                foreach (var chr in title)
                {
                    if (!font.TryGetCharMetrics(chr, UIScale, out var metrics))
                    {
                        continue;
                    }

                    titleLength += metrics.Advance;
                }

                var active = _currentTab == i;
                var box = active ? boxActive : boxInactive;
                var boxAdvance = titleLength + box?.MinimumSize.X ?? 0;

                if (headerOffset < relX && headerOffset + boxAdvance > relX)
                {
                    // Got em.
                    CurrentTab = i;
                    return;
                }

                headerOffset += boxAdvance;
            }
        }

        [System.Diagnostics.Contracts.Pure]
        private UIBox2i _getContentBox()
        {
            var headerSize = _getHeaderSize();
            var panel = _getPanel();
            var panelBox = new UIBox2i(0, headerSize, PixelWidth, PixelHeight);
            if (panel != null)
            {
                return (UIBox2i) panel.GetContentBox(panelBox);
            }
            return panelBox;
        }

        [System.Diagnostics.Contracts.Pure]
        private int _getHeaderSize()
        {
            var headerSize = 0;

            if (TabsVisible)
            {
                var active = _getTabBoxActive();
                var inactive = _getTabBoxInactive();
                var font = _getFont();

                var activeSize = active?.MinimumSize ?? Vector2.Zero;
                var inactiveSize = inactive?.MinimumSize ?? Vector2.Zero;

                headerSize = (int) MathF.Max(activeSize.Y, inactiveSize.Y);
                headerSize += font.GetHeight(UIScale);
            }

            return headerSize;
        }

        [System.Diagnostics.Contracts.Pure]
        private StyleBox? _getTabBoxActive()
        {
            TryGetStyleProperty<StyleBox>(StylePropertyTabStyleBox, out var box);
            return box;
        }

        [System.Diagnostics.Contracts.Pure]
        private StyleBox? _getTabBoxInactive()
        {
            TryGetStyleProperty<StyleBox>(StylePropertyTabStyleBoxInactive, out var box);
            return box;
        }

        [System.Diagnostics.Contracts.Pure]
        private Color _getTabFontColorActive()
        {
            if (TryGetStyleProperty(stylePropertyTabFontColor, out Color color))
            {
                return color;
            }
            return Color.White;
        }

        [System.Diagnostics.Contracts.Pure]
        private Color _getTabFontColorInactive()
        {
            if (TryGetStyleProperty(StylePropertyTabFontColorInactive, out Color color))
            {
                return color;
            }
            return Color.Gray;
        }

        [System.Diagnostics.Contracts.Pure]
        private StyleBox? _getPanel()
        {
            TryGetStyleProperty<StyleBox>(StylePropertyPanelStyleBox, out var box);
            return box;
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
    }
}
