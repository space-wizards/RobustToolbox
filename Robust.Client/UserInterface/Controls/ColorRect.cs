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

            Color? color = null;
            color = _color ?? (TryGetStyleProperty("color", out Color styleColor) ? styleColor : DefaultColor);

            if (color != null)
                handle.DrawRect(PixelSizeBox, (Color)color);
            else
                throw new InvalidOperationException("Tried to draw a colorrect with no color! This means the defaultcolor failed too!");
        }

        protected override Vector2 CalculateMinimumSize()
            => Vector2.One;

    }
}
