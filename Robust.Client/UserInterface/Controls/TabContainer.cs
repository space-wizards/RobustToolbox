using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Input;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    [Virtual]
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

        // the laid out tabs
        private List<TabBox> _tabBoxes = new();
        private float _enclosingTabHeight;

        public int CurrentTab
        {
            get => _currentTab;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Current tab must be positive.");
                }

                if (value >= ChildCount)
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
                InvalidateMeasure();

                OnTabChanged?.Invoke(value);
            }
        }

        public bool TabsVisible
        {
            get => _tabsVisible;
            set
            {
                _tabsVisible = value;
                InvalidateMeasure();
            }
        }

        public StyleBox? PanelStyleBoxOverride { get; set; }
        public Color? TabFontColorOverride { get; set; }
        public Color? TabFontColorInactiveOverride { get; set; }

        public event Action<int>? OnTabChanged;

        public TabContainer()
        {
            MouseFilter = MouseFilterMode.Pass;
        }

        public string GetActualTabTitle(int tab)
        {
            var control = GetChild(tab);
            var title = control.GetValue(TabTitleProperty);

            return title ?? control.Name ?? Loc.GetString("tab-container-not-tab-title-provided");
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
            var headerSize = _enclosingTabHeight;
            var panel = _getPanel();
            var panelBox = new UIBox2(0, headerSize, PixelWidth, PixelHeight);

            panel?.Draw(handle, panelBox, UIScale);

            var font = _getFont();
            var fontColorActive = _getTabFontColorActive();
            var fontColorInactive = _getTabFontColorInactive();

            // Then draw the tabs
            foreach (var tabBox in _tabBoxes)
            {
                if (tabBox.Box is { } styleBox)
                {
                    styleBox.Draw(handle, tabBox.Bounding, UIScale);
                }

                var baseLine = new Vector2(0, font.GetAscent(UIScale)) + tabBox.Content.TopLeft;
                foreach (var rune in tabBox.Title.EnumerateRunes())
                {
                    if (!font.TryGetCharMetrics(rune, UIScale, out var metrics))
                        continue;

                    font.DrawChar(handle, rune, baseLine, UIScale, tabBox.Index == _currentTab ? fontColorActive : fontColorInactive);
                    baseLine += new Vector2(metrics.Advance, 0);
                }
            }
        }

        private readonly record struct TabBox(UIBox2 Bounding, UIBox2 Content, StyleBox? Box, string Title, int Index);

        private void CalculateTabBoxes(Vector2 availableSize)
        {
            availableSize *= UIScale;
            var tabLeft = 0f;
            var tabTop = 0f;
            var tabHeight = 0f;

            var font = _getFont();
            var boxActive = _getTabBoxActive();
            var boxInactive = _getTabBoxInactive();

            _tabBoxes.Clear();

            if (!_tabsVisible)
                return;

            for (var i = 0; i < ChildCount; i++)
            {
                if (!GetTabVisible(i))
                    continue;

                var title = GetActualTabTitle(i);

                var titleLength = 0;
                foreach (var rune in title.EnumerateRunes())
                {
                    if (!font.TryGetCharMetrics(rune, UIScale, out var metrics))
                        continue;

                    titleLength += metrics.Advance;
                }

                var active = _currentTab == i;
                var box = active ? boxActive : boxInactive;

                var topLeft = new Vector2(tabLeft, tabTop);
                var size = new Vector2(titleLength, font.GetHeight(UIScale));

                if (box != null)
                {
                    size = box.GetEnvelopBox(topLeft, size, UIScale).Size;
                }

                if (tabLeft + size.X > availableSize.X)
                {
                    tabLeft = 0;
                    tabTop += tabHeight;
                    tabHeight = 0;
                }

                topLeft = new(tabLeft, tabTop);
                size = new(titleLength, font.GetHeight(UIScale));

                UIBox2 boundingBox;
                UIBox2 contentBox;
                if (box != null)
                {
                    boundingBox = box.GetEnvelopBox(topLeft, size, UIScale);
                    contentBox = box.GetContentBox(boundingBox, UIScale);
                }
                else
                {
                    contentBox = UIBox2.FromDimensions(topLeft, size);
                    boundingBox = contentBox;
                }

                tabLeft += boundingBox.Size.X;
                tabHeight = Math.Max(tabHeight, boundingBox.Size.Y);
                _tabBoxes.Add(new(boundingBox, contentBox, box, title, i));
            }

            if (Math.Abs(_enclosingTabHeight - (tabTop + tabHeight)) >= 0.1)
            {
                InvalidateArrange();
            }
            _enclosingTabHeight = tabTop + tabHeight;
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            CalculateTabBoxes(availableSize);
            var headerSize = Vector2.Zero;

            if (TabsVisible)
            {
                headerSize = new Vector2(0, _enclosingTabHeight / UIScale);
            }

            var panel = _getPanel();
            var panelSize = (panel?.MinimumSize ?? Vector2.Zero);

            var contentsSize = availableSize - headerSize - panelSize;

            var total = Vector2.Zero;
            foreach (var child in Children)
            {
                if (child.Visible)
                {
                    child.Measure(contentsSize);
                    total = Vector2.Max(child.DesiredSize, total);
                }
            }

            return total + headerSize + panelSize;
        }

        protected override Vector2 ArrangeOverride(Vector2 finalSize)
        {
            CalculateTabBoxes(finalSize);
            if (ChildCount == 0 || _currentTab >= ChildCount)
            {
                return finalSize;
            }

            var headerSize = (int)_enclosingTabHeight;
            var panel = _getPanel();
            var contentBox = new UIBox2i(0, headerSize, (int) (finalSize.X * UIScale), (int) (finalSize.Y * UIScale));
            if (panel != null)
            {
                contentBox = (UIBox2i) panel.GetContentBox(contentBox, UIScale);
            }

            var control = GetChild(_currentTab);
            control.Visible = true;
            control.ArrangePixel(contentBox);
            return finalSize;
        }

        protected internal override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            base.KeyBindDown(args);

            if (!TabsVisible || args.Function != EngineKeyFunctions.UIClick)
            {
                return;
            }

            // Outside of header size, ignore.
            if (args.RelativePixelPosition.Y < 0 || args.RelativePixelPosition.Y > _enclosingTabHeight)
            {
                return;
            }

            args.Handle();

            foreach (var box in _tabBoxes)
            {
                if (box.Bounding.Contains(args.RelativePixelPosition))
                {
                    CurrentTab = box.Index;
                    return;
                }
            }
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
            if (TabFontColorOverride != null)
                return TabFontColorOverride.Value;

            if (TryGetStyleProperty(stylePropertyTabFontColor, out Color color))
            {
                return color;
            }
            return Color.White;
        }

        [System.Diagnostics.Contracts.Pure]
        private Color _getTabFontColorInactive()
        {
            if (TabFontColorInactiveOverride != null)
                return TabFontColorInactiveOverride.Value;

            if (TryGetStyleProperty(StylePropertyTabFontColorInactive, out Color color))
            {
                return color;
            }
            return Color.Gray;
        }

        [System.Diagnostics.Contracts.Pure]
        private StyleBox? _getPanel()
        {
            if (PanelStyleBoxOverride != null)
                return PanelStyleBoxOverride;

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
