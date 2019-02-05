using System;
using System.Linq;
using JetBrains.Annotations;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Drawing;
using SS14.Shared.Maths;
using SS14.Shared.Utility;

namespace SS14.Client.UserInterface.Controls
{
    /// <summary>
    ///     A label is a GUI control that displays simple text.
    /// </summary>
    [ControlWrap(typeof(Godot.Label))]
    public class Label : Control
    {
        private Vector2i? _textDimensionCache;

        public Label(string name) : base(name)
        {
        }

        public Label() : base()
        {
        }

        internal Label(Godot.Label control) : base(control)
        {
        }

        private string _text;

        public string Text
        {
            get => GameController.OnGodot ? (string) SceneControl.Get("text") : _text;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("text", value);
                }
                else
                {
                    _text = value;
                    _textDimensionCache = null;
                    MinimumSizeChanged();
                }
            }
        }

        public bool AutoWrap
        {
            get => GameController.OnGodot ? (bool) SceneControl.Get("autowrap") : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("autowrap", value);
                }
            }
        }

        public AlignMode Align
        {
            get => GameController.OnGodot ? (AlignMode) SceneControl.Get("align") : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("align", (Godot.Label.AlignEnum) value);
                }
            }
        }

        private Font ActiveFont => _fontOverride ?? UserInterfaceManager.DefaultFont;
        private Font _fontOverride;

        public Font FontOverride
        {
            get => _fontOverride ?? GetFontOverride("font");
            set => SetFontOverride("font", _fontOverride = value);
        }

        private Color? _fontColorShadowOverride;

        public Color? FontColorShadowOverride
        {
            get => _fontColorShadowOverride ?? GetColorOverride("font_color_shadow");
            set => SetColorOverride("font_color_shadow", _fontColorShadowOverride = value);
        }

        private Color? _fontColorOverride;

        public Color? FontColorOverride
        {
            get => _fontColorOverride ?? GetColorOverride("font_color");
            set => SetColorOverride("font_color", _fontColorOverride = value);
        }

        private int? _shadowOffsetXOverride;

        public int? ShadowOffsetXOverride
        {
            get => _shadowOffsetXOverride ?? GetConstantOverride("shadow_offset_x");
            set => SetConstantOverride("shadow_offset_x", _shadowOffsetXOverride = value);
        }

        private int? _shadowOffsetYOverride;

        public int? ShadowOffsetYOverride
        {
            get => _shadowOffsetYOverride ?? GetConstantOverride("shadow_offset_y");
            set => SetConstantOverride("shadow_offset_y", _shadowOffsetYOverride = value);
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.Label();
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            if (GameController.OnGodot)
            {
                return;
            }

            if (_text == null)
            {
                return;
            }

            var newlines = 0;
            var baseLine = new Vector2(0, ActiveFont.Ascent);
            foreach (var chr in _text)
            {
                if (chr == '\n')
                {
                    newlines += 1;
                    baseLine = new Vector2(0, ActiveFont.Ascent + ActiveFont.Height * newlines);
                }

                var advance = ActiveFont.DrawChar(handle, chr, baseLine, Color.White);
                baseLine += new Vector2(advance, 0);
            }
        }


        public enum AlignMode
        {
            Left = 0,
            Center = 1,
            Right = 2,
            Fill = 3,
        }

        protected override Vector2 CalculateMinimumSize()
        {
            if (GameController.OnGodot)
            {
                return base.CalculateMinimumSize();
            }

            if (!_textDimensionCache.HasValue)
            {
                _calculateTextDimension();
                DebugTools.Assert(_textDimensionCache.HasValue);
            }

            return _textDimensionCache.Value;
        }

        private void _calculateTextDimension()
        {
            if (_text == null)
            {
                _textDimensionCache = Vector2i.Zero;
                return;
            }

            var height = ActiveFont.Ascent;
            var maxLineSize = 0;
            var currentLineSize = 0;
            foreach (var chr in _text)
            {
                if (chr == '\n')
                {
                    maxLineSize = Math.Max(currentLineSize, maxLineSize);
                    currentLineSize = 0;
                    height += ActiveFont.Height;
                }
                else
                {
                    var metrics = ActiveFont.GetCharMetrics(chr);
                    if (!metrics.HasValue)
                    {
                        continue;
                    }

                    currentLineSize += metrics.Value.Advance;
                }
            }
            maxLineSize = Math.Max(currentLineSize, maxLineSize);

            _textDimensionCache = new Vector2i(maxLineSize, height);
        }
    }
}
