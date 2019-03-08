using System;
using System.Diagnostics.Contracts;
using SS14.Client.Graphics.Drawing;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.ProgressBar))]
    public class ProgressBar : Range
    {
        public const string StylePropertyBackground = "background";
        public const string StylePropertyForeground = "foreground";

        public ProgressBar()
        {
        }

        public ProgressBar(string name) : base(name)
        {
        }

        internal ProgressBar(Godot.ProgressBar control) : base(control)
        {
        }

        /// <summary>
        ///     True if the percentage label on top of the progress bar is visible.
        /// </summary>
        public bool PercentVisible
        {
            get => GameController.OnGodot ? (bool)SceneControl.Get("percent_visible") : default;
            set
            {
                if (GameController.OnGodot) SceneControl.Set("percent_visible", value);
            }
        }

        [Pure]
        private StyleBox _getBackground()
        {
            TryGetStyleProperty(StylePropertyBackground, out StyleBox ret);
            return ret;
        }

        [Pure]
        private StyleBox _getForeground()
        {
            TryGetStyleProperty(StylePropertyForeground, out StyleBox ret);
            return ret;
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            if (GameController.OnGodot)
            {
                return;
            }

            var bg = _getBackground();
            bg?.Draw(handle, SizeBox);

            var fg = _getForeground();
            if (fg == null)
            {
                return;
            }
            var minSize = fg.MinimumSize;
            var size = Width * GetAsRatio() - minSize.X;
            if (size > 0)
            {
                fg.Draw(handle, UIBox2.FromDimensions(0, 0, minSize.X + size, Height));
            }
        }

        protected override Vector2 CalculateMinimumSize()
        {
            if (GameController.OnGodot)
            {
                return Vector2.Zero;
            }

            var bgSize = _getBackground()?.MinimumSize ?? Vector2.Zero;
            var fgSize = _getForeground()?.MinimumSize ?? Vector2.Zero;

            return Vector2.ComponentMax(bgSize, fgSize);
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.ProgressBar();
        }
    }
}
