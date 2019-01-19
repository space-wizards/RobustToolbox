using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.Button))]
    public class Button : BaseButton
    {
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

        public AlignMode TextAlign
        {
            get => GameController.OnGodot ? (AlignMode)SceneControl.Get("align") : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("align", (Godot.Button.TextAlign) value);
                }
            }
        }

        public bool ClipText
        {
            get => GameController.OnGodot ? (bool)SceneControl.Get("clip_text") : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("clip_text", value);
                }
            }
        }

        public bool Flat
        {
            get => GameController.OnGodot ? (bool)SceneControl.Get("flat") : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("flat", value);
                }
            }
        }

        public string Text
        {
            get => GameController.OnGodot ? (string)SceneControl.Get("text") : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("text", value);
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
    }
}
