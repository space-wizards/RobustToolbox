using SFML.Graphics;
using SFML.System;
using OpenTK;
using OpenTK.Graphics;
using SS14.Client.Graphics.Utility;
using Vector2i = SS14.Shared.Maths.Vector2i;
using SS14.Shared.Maths;
using Vector2 = SS14.Shared.Maths.Vector2;
using Color = SS14.Shared.Maths.Color;
using SS14.Client.Graphics.Render;
using System;
using STransformable = SFML.Graphics.Transformable;
using RenderStates = SS14.Client.Graphics.Render.RenderStates;

namespace SS14.Client.Graphics.Sprites
{
    /// <summary>
    /// Sprite that contains Text
    /// </summary>
    public class TextSprite : Transformable, IDrawable
    {
        [Flags]
        public enum Styles
        {
            None = 0,
            Bold = 1,
            Italic = 1 << 1,
            Underlined = 1 << 2,
            StrikeThrough = 1 << 3,
        }

        public Text SFMLTextSprite { get; }

        public Drawable SFMLDrawable => SFMLTextSprite;
        public override STransformable SFMLTransformable => SFMLTextSprite;

        #region Constructors

        /// <summary>
        /// Creates a TextSprite
        /// </summary>
        /// <param name="Label"> Label of TextSprite </param>
        /// <param name="text"> Text to display </param>
        /// <param name="font"> Font to use when displaying Text </param>
        /// <param name="font"> Size of the font to use </param>
        public TextSprite(string text, Font font, uint size)
        {
            SFMLTextSprite = new Text(text, font.SFMLFont, size);
            CalculateOrigin();
        }

        /// <summary>
        /// Creates a TextSprite
        /// </summary>
        /// <param name="Label"> Label of TextSprite </param>
        /// <param name="text"> Text to display </param>
        /// <param name="font"> Font to use when displaying Text </param>
        public TextSprite(string text, Font font) : this(text, font, 14) { }

        /// <summary>
        /// Draws the TextSprite to the CurrentRenderTarget
        /// </summary>
        ///

        #endregion Constructors

        #region Methods

        public void Draw(IRenderTarget target, RenderStates states)
        {
            if (Shadowed)
            {
                var oldPos = SFMLTextSprite.Position;
                var oldColor = SFMLTextSprite.FillColor;
                SFMLTextSprite.Position += ShadowOffset.Convert();
                SFMLTextSprite.FillColor = ShadowColor.Convert();
                SFMLTextSprite.Draw(target.SFMLTarget, states.SFMLRenderStates);
                SFMLTextSprite.Position = oldPos;
                SFMLTextSprite.FillColor = oldColor;
            }
            SFMLTextSprite.Draw(target.SFMLTarget, states.SFMLRenderStates);

            if (CluwneLib.Debug.DebugTextboxes)
            {
                var fr = SFMLTextSprite.GetGlobalBounds().Convert();
                CluwneLib.drawHollowRectangle((int)fr.Left, (int)fr.Top, (int)fr.Width, (int)fr.Height, 1.0f, Color.Red);
            }
        }

        public void Draw()
        {
            Draw(CluwneLib.CurrentRenderTarget, CluwneLib.ShaderRenderState);
        }

        /// <summary>
        /// Get the length, in pixels, that the provided string would be.
        /// </summary>
        public int MeasureLine(string _text)
        {
            string temp = Text;
            Text = _text;
            int value = (int)SFMLTextSprite.FindCharacterPos((uint)SFMLTextSprite.DisplayedString.Length + 1).X;
            Text = temp;
            return value;
        }

        /// <summary>
        /// Get the length, in pixels, of this TextSprite.
        /// </summary>
        public int MeasureLine()
        {
            return MeasureLine(Text);
        }

        public Vector2 FindCharacterPos(uint index)
        {
            return SFMLTextSprite.FindCharacterPos(index).Convert();
        }

        #endregion Methods

        #region Accessors

        public Color ShadowColor { get; set; } = Color.Black;
        public bool Shadowed { get; set; } = false;
        public Vector2 ShadowOffset { get; set; } = new Vector2(1, 1);

        public Color FillColor
        {
            get => SFMLTextSprite.FillColor.Convert();
            set => SFMLTextSprite.FillColor = value.Convert();
        }

        public uint FontSize
        {
            get => SFMLTextSprite.CharacterSize;
            set => SFMLTextSprite.CharacterSize = value;
        }

        public string Text
        {
            get => SFMLTextSprite.DisplayedString;
            set
            {
                SFMLTextSprite.DisplayedString = value;
                CalculateOrigin();
            }
        }

        public Styles Style
        {
            get => (Styles)SFMLTextSprite.Style;
            set => SFMLTextSprite.Style = (Text.Styles)value;
        }

        public int Width => (int)SFMLTextSprite.GetLocalBounds().Width;

        // FIXME take into account newlines.
        public int Height => (int)SFMLTextSprite.GetLocalBounds().Height;

        /// <summary>
        ///     Sets up positioning so that top left bounds are the origin of the text, instead of the left end of the font baseline.
        ///     This needs to be recalculated whenever the text changes. Keep in mind this will change the origin of rotation.
        /// </summary>
        private void CalculateOrigin()
        {
            var bounds = SFMLTextSprite.GetLocalBounds();
            SFMLTextSprite.Origin = new Vector2f(bounds.Left, bounds.Top);
        }

        #endregion Accessors
    }
}
