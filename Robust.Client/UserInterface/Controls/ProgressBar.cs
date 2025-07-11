using System.Diagnostics.Contracts;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    [Virtual]
    public class ProgressBar : Range
    {
        public const string StylePropertyBackground = "background";
        public const string StylePropertyForeground = "foreground";

        private StyleBox? _backgroundStyleBoxOverride;
        private StyleBox? _foregroundStyleBoxOverride;

        private bool _vertical;

        /// <summary>
        /// Whether the progress bar is oriented vertically.
        /// </summary>
        /// <remarks>
        /// A vertical progress bar fills from bottom to top.
        /// </remarks>
        public bool Vertical
        {
            get => _vertical;
            set
            {
                if (_vertical != value)
                {
                    _vertical = value;
                    InvalidateMeasure();
                }
            }
        }

        public StyleBox? BackgroundStyleBoxOverride
        {
            get => _backgroundStyleBoxOverride;
            set
            {
                _backgroundStyleBoxOverride = value;
                InvalidateMeasure();
            }
        }

        public StyleBox? ForegroundStyleBoxOverride
        {
            get => _foregroundStyleBoxOverride;
            set
            {
                _foregroundStyleBoxOverride = value;
                InvalidateMeasure();
            }
        }

        [Pure]
        private StyleBox? _getBackground()
        {
            if (BackgroundStyleBoxOverride != null)
            {
                return BackgroundStyleBoxOverride;
            }

            TryGetStyleProperty<StyleBox>(StylePropertyBackground, out var ret);
            return ret;
        }

        [Pure]
        private StyleBox? _getForeground()
        {
            if (ForegroundStyleBoxOverride != null)
            {
                return ForegroundStyleBoxOverride;
            }

            TryGetStyleProperty<StyleBox>(StylePropertyForeground, out var ret);
            return ret;
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            var bg = _getBackground();
            bg?.Draw(handle, PixelSizeBox, UIScale);

            var fg = _getForeground();
            if (fg == null)
            {
                return;
            }

            if (_vertical)
            {
                var size = PixelHeight * GetAsRatio();
                if (size > 0)
                {
                    fg.Draw(handle, UIBox2.FromDimensions(0, PixelHeight - size, PixelWidth, size), UIScale);
                }
            }
            else
            {
                var minSize = fg.MinimumSize;
                var size = PixelWidth * GetAsRatio() - minSize.X;
                if (size > 0)
                {
                    fg.Draw(handle, UIBox2.FromDimensions(0, 0, minSize.X + size, PixelHeight), UIScale);
                }
            }
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            var bgSize = _getBackground()?.MinimumSize ?? Vector2.Zero;
            var fgSize = _getForeground()?.MinimumSize ?? Vector2.Zero;

            return Vector2.Max(bgSize, fgSize);
        }
    }
}
