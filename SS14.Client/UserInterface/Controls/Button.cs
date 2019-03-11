using System;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Drawing;
using SS14.Client.Utility;
using SS14.Shared.Maths;
using SS14.Shared.ViewVariables;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.Button))]
    public class Button : BaseButton
    {
        public const string StylePropertyStyleBox = "stylebox";
        public const string StylePseudoClassNormal = "normal";
        public const string StylePseudoClassHover = "hover";
        public const string StylePseudoClassDisabled = "disabled";
        public const string StylePseudoClassPressed = "pressed";
        private int? _textWidthCache;

        public Button()
        {
        }

        public Button(string name) : base(name)
        {
        }

        internal Button(Godot.Button button) : base(button)
        {
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.Button();
        }

        private AlignMode _textAlign;
        [ViewVariables]
        public AlignMode TextAlign
        {
            get => GameController.OnGodot ? (AlignMode)SceneControl.Get("align") : _textAlign;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("align", (Godot.Button.TextAlign) value);
                }
                else
                {
                    _textAlign = value;
                }
            }
        }

        private bool _clipText;
        [ViewVariables]
        public bool ClipText
        {
            get => GameController.OnGodot ? (bool)SceneControl.Get("clip_text") : _clipText;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("clip_text", value);
                }
                else
                {
                    _clipText = value;
                }
            }
        }

        private string _text;
        [ViewVariables]
        public string Text
        {
            get => GameController.OnGodot ? (string)SceneControl.Get("text") : _text;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("text", value);
                }
                else
                {
                    _text = value;
                    _textWidthCache = null;
                    MinimumSizeChanged();
                }
            }
        }

        private StyleBox ActualStyleBox
        {
            get
            {
                if (TryGetStyleProperty(StylePropertyStyleBox, out StyleBox box))
                {
                    return box;
                }

                return UserInterfaceManager.ThemeDefaults.ButtonStyle;
            }
        }

        public Font ActualFont
        {
            get
            {
                if (TryGetStyleProperty("font", out Font font))
                {
                    return font;
                }

                return UserInterfaceManager.ThemeDefaults.DefaultFont;
            }
        }

        public Color ActualFontColor
        {
            get
            {
                if (TryGetStyleProperty("font-color", out Color fontColor))
                {
                    return fontColor;
                }

                return _fontColorOverride ?? Color.White;
            }
        }

        private Color? _fontColorOverride;

        public Color? FontColorOverride
        {
            get => _fontColorOverride ?? GetColorOverride("font_color");
            set => SetColorOverride("font_color", _fontColorOverride = value);
        }

        private Color? _fontColorDisabledOverride;

        public Color? FontColorDisabledOverride
        {
            get => _fontColorDisabledOverride ?? GetColorOverride("font_color_disabled");
            set => SetColorOverride("font_color_disabled", _fontColorDisabledOverride = value);
        }

        private Color? _fontColorHoverOverride;

        public Color? FontColorHoverOverride
        {
            get => _fontColorHoverOverride ?? GetColorOverride("font_color_hover");
            set => SetColorOverride("font_color_hover", _fontColorHoverOverride = value);
        }

        private Color? _fontColorPressedOverride;

        public Color? FontColorPressedOverride
        {
            get => _fontColorPressedOverride ?? GetColorOverride("font_color_pressed");
            set => SetColorOverride("font_color_pressed", _fontColorPressedOverride = value);
        }

        public enum AlignMode
        {
            Left = 0,
            Center = 1,
            Right = 2,
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            if (GameController.OnGodot)
            {
                return;
            }

            var style = ActualStyleBox;
            var drawBox = SizeBox;
            style.Draw(handle, drawBox);

            if (_text == null)
            {
                return;
            }

            var contentBox = style.GetContentBox(drawBox);
            DrawTextInternal(handle, contentBox);
        }

        protected void DrawTextInternal(DrawingHandleScreen handle, UIBox2 box)
        {
            var width = EnsureWidthCache();
            var font = ActualFont;
            int drawOffset;

            switch (TextAlign)
            {
                case AlignMode.Left:
                    drawOffset = 0;
                    break;
                case AlignMode.Center:
                    drawOffset = (int)(box.Width - width) / 2;
                    break;
                case AlignMode.Right:
                    drawOffset = (int)(box.Width - width);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var color = ActualFontColor;
            var offsetY = (int) (box.Height - font.Height) / 2;
            var baseLine = new Vector2i(drawOffset, offsetY+font.Ascent) + box.TopLeft;

            foreach (var chr in _text)
            {
                if (!font.TryGetCharMetrics(chr, out var metrics))
                {
                    continue;
                }

                if (!(ClipText && (baseLine.X < box.Left || baseLine.X + metrics.Advance > box.Right)))
                {
                    font.DrawChar(handle, chr, baseLine, color);
                }
                baseLine += (metrics.Advance, 0);
            }
        }

        protected override Vector2 CalculateMinimumSize()
        {
            if (GameController.OnGodot)
            {
                return base.CalculateMinimumSize();
            }

            var uiTheme = UserInterfaceManager.ThemeDefaults;
            var style = ActualStyleBox;
            var font = ActualFont;

            var fontHeight = font.Height;

            if (ClipText)
            {
                return (0, fontHeight) + style.MinimumSize;
            }

            var width = EnsureWidthCache();

            return new Vector2(width, fontHeight) + style.MinimumSize;
        }

        protected override void Initialize()
        {
            base.Initialize();

            DrawModeChanged();
        }

        protected override void DrawModeChanged()
        {
            switch (DrawMode)
            {
                case DrawModeEnum.Normal:
                    StylePseudoClass = StylePseudoClassNormal;
                    break;
                case DrawModeEnum.Pressed:
                    StylePseudoClass = StylePseudoClassPressed;
                    break;
                case DrawModeEnum.Hover:
                    StylePseudoClass = StylePseudoClassHover;
                    break;
                case DrawModeEnum.Disabled:
                    StylePseudoClass = StylePseudoClassDisabled;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected int EnsureWidthCache()
        {
            if (_textWidthCache.HasValue)
            {
                return _textWidthCache.Value;
            }

            if (_text == null)
            {
                _textWidthCache = 0;
                return 0;
            }

            var font = ActualFont;

            var textWidth = 0;
            foreach (var chr in _text)
            {
                var metrics = font.GetCharMetrics(chr);
                if (metrics == null)
                {
                    continue;
                }

                textWidth += metrics.Value.Advance;
            }

            _textWidthCache = textWidth;
            return textWidth;
        }

        protected override void StylePropertiesChanged()
        {
            _textWidthCache = null;

            base.StylePropertiesChanged();
        }

        private protected override void SetGodotProperty(string property, object value, GodotAssetScene context)
        {
            base.SetGodotProperty(property, value, context);

            if (property == "text")
            {
                Text = (string) value;
            }

            if (property == "align")
            {
                TextAlign = (AlignMode) (long) value;
            }
        }
    }
}
