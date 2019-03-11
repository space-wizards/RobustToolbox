using SS14.Client.Graphics;
using SS14.Client.Graphics.Drawing;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.CheckBox))]
    public class CheckBox : Button
    {
        public const string StylePropertyIcon = "icon";
        public const string StylePropertyHSeparation = "hseparation";

        public CheckBox()
        {
        }

        public CheckBox(string name) : base(name)
        {
        }

        internal CheckBox(Godot.CheckBox box) : base(box)
        {
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            if (GameController.OnGodot)
            {
                return;
            }

            var offset = 0;
            var icon = _getIcon();
            if (icon != null)
            {
                offset += _getIcon().Width + _getHSeparation();
                handle.DrawTexture(icon, Vector2.Zero);
            }

            var box = new UIBox2(offset, 0, Width, Height);
            DrawTextInternal(handle, box);
        }

        protected override Vector2 CalculateMinimumSize()
        {
            if (GameController.OnGodot)
            {
                return Vector2.Zero;
            }

            var minSize = _getIcon()?.Size ?? Vector2i.Zero;
            var font = ActualFont;

            if (!string.IsNullOrWhiteSpace(Text) && !ClipText)
            {
                minSize += new Vector2i(EnsureWidthCache() + _getHSeparation(), 0);
            }
            minSize = Vector2i.ComponentMax(minSize, new Vector2i(0, font.Height));

            return minSize;
        }

        private Texture _getIcon()
        {
            if (TryGetStyleProperty(StylePropertyIcon, out Texture tex))
            {
                return tex;
            }

            return null;
        }

        private int _getHSeparation()
        {
            if (TryGetStyleProperty(StylePropertyHSeparation, out int hSep))
            {
                return hSep;
            }

            return 0;
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.CheckBox();
        }

        protected override void SetDefaults()
        {
            base.SetDefaults();

            ToggleMode = true;
        }
    }
}
