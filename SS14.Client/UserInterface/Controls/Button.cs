using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Client.Graphics.Drawing;
using SS14.Client.Utility;
using SS14.Shared.Maths;
using SS14.Shared.Utility;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.Button))]
    public class Button : BaseButton
    {
        private int? _textWidthCache;

        public Button() : base()
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

            var uiTheme = UserInterfaceManager.Theme;
            StyleBox style;
            switch (DrawMode)
            {
                case DrawModeEnum.Normal:
                    style = uiTheme.ButtonStyleNormal;
                    break;
                case DrawModeEnum.Pressed:
                    style = uiTheme.ButtonStylePressed;
                    break;
                case DrawModeEnum.Disabled:
                    style = uiTheme.ButtonStyleDisabled;
                    break;
                case DrawModeEnum.Hover:
                    style = uiTheme.ButtonStyleHovered;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            var font = uiTheme.DefaultFont;
            var drawBox = UIBox2.FromDimensions(Vector2.Zero, Size);
            style.Draw(handle, drawBox);

            if (_text == null)
            {
                return;
            }

            var width = _ensureWidthCache();
            var contentBox = style.GetContentBox(drawBox);
            int drawOffset;

            switch (TextAlign)
            {
                case AlignMode.Left:
                    drawOffset = 0;
                    break;
                case AlignMode.Center:
                    drawOffset = (int)(contentBox.Width - width) / 2;
                    break;
                case AlignMode.Right:
                    drawOffset = (int)(contentBox.Width - width);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // ReSharper disable once PossibleLossOfFraction
            var baseLine = new Vector2(drawOffset, (int)(contentBox.Height + font.Ascent)/2) + contentBox.TopLeft;

            foreach (var chr in _text)
            {
                var advance = font.DrawChar(handle, chr, baseLine, Color.White);
                baseLine += new Vector2(advance, 0);
            }
        }

        protected override Vector2 CalculateMinimumSize()
        {
            if (GameController.OnGodot)
            {
                return base.CalculateMinimumSize();
            }

            var uiTheme = UserInterfaceManager.Theme;
            var style = uiTheme.ButtonStyleNormal;
            var font = uiTheme.DefaultFont;

            var fontHeight = font.Ascent;

            var width = _ensureWidthCache();

            return new Vector2(width, fontHeight) + style.MinimumSize;
        }

        private int _ensureWidthCache()
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

            var uiTheme = UserInterfaceManager.Theme;
            var font = uiTheme.DefaultFont;

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
