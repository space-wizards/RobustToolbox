using SS14.Client.Graphics;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    /// <summary>
    ///     A label is a GUI control that displays simple text.
    /// </summary>
    [ControlWrap(typeof(Godot.Label))]
    public class Label : Control
    {
        public Label(string name) : base(name)
        {
        }
        public Label() : base()
        {
        }
        public Label(Godot.Label control) : base(control)
        {
        }

        public string Text
        {
            get => SceneControl.Text;
            set => SceneControl.Text = value;
        }

        public bool AutoWrap
        {
            get => SceneControl.Autowrap;
            set => SceneControl.Autowrap = value;
        }

        public AlignMode Align
        {
            get => (AlignMode) SceneControl.Align;
            set => SceneControl.Align = (Godot.Label.AlignEnum) value;
        }

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

        new private Godot.Label SceneControl;

        protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.Label();
        }

        protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(control);
            SceneControl = (Godot.Label)control;
        }

        public enum AlignMode
        {
            Left = Godot.Label.AlignEnum.Left,
            Center = Godot.Label.AlignEnum.Center,
            Right = Godot.Label.AlignEnum.Right,
            Fill = Godot.Label.AlignEnum.Fill,
        }
    }
}
