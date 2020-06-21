using System;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Shaders;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     Simple control that draws a colored rectangle
    /// </summary>
    public class ColorRect : Control
    {

        private static readonly Color DefaultColor = new Color(0,0,0, 0);

        private Color? _color;

        /// <summary>
        ///     The color to draw.
        /// </summary>
        public Color? Color
        {
            get => _color;
            set
            {
                _color = value;
                MinimumSizeChanged();
            }
        }

        /// <summary>
        ///     The shader to use.
        /// </summary>
        public ShaderInstance? Shader
        {
            get;
            set;
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            handle.UseShader(Shader);

            if (!TryGetStyleProperty("color", out Color color))
            {
                color = _color ?? DefaultColor;
            }

            handle.DrawRect(UIBox2.FromDimensions(Vector2.Zero, PixelSize), color);
        }

        protected override Vector2 CalculateMinimumSize()
            => Vector2.One;

    }
}
