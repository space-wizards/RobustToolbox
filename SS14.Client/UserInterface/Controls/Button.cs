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

        new private Godot.Button SceneControl;

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.Button();
        }

        private protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(control);
            SceneControl = (Godot.Button)control;
        }

        public AlignMode TextAlign
        {
            get => (AlignMode)SceneControl.Align;
            set => SceneControl.Align = (Godot.Button.TextAlign)value;
        }

        public bool ClipText
        {
            get => SceneControl.ClipText;
            set => SceneControl.ClipText = value;
        }

        public bool Flat
        {
            get => SceneControl.Flat;
            set => SceneControl.Flat = value;
        }

        public string Text
        {
            get => SceneControl.Text;
            set => SceneControl.Text = value;
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
            Left = Godot.Button.TextAlign.Left,
            Center = Godot.Button.TextAlign.Center,
            Right = Godot.Button.TextAlign.Right,
        }
    }
}
