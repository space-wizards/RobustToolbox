using System;
using OpenTK.Graphics;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Sprite;
using SS14.Client.Interfaces.Resource;
using SS14.Client.ResourceManagement;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Components
{
    internal class Label : Control
    {
        public delegate void LabelPressHandler(Label sender, MouseButtonEventArgs e);

        private const uint DefaultFontSize = 14;

        public int FixedHeight { get; set; } = -1;
        public int FixedWidth { get; set; } = -1;
        public Color4 HighlightColor { get; set; } = Color4.Gray;
        private TextSprite _text;

        public uint FontSize
        {
            get => _text.FontSize;
            set => _text.FontSize = value;
        }

        public string Text
        {
            get => _text.Text;
            set => _text.Text = value;
        }

        public override Color4 ForegroundColor
        {
            get => _text.Color;
            set => _text.Color = value;
        }
        
        public bool DrawTextHighlight { get; set; }

        public Label(string text, string font, uint size = DefaultFontSize)
        {
            _text = new TextSprite(text, _resourceCache.GetResource<FontResource>($"Fonts/{font}.TTF"), size)
            {
                Color = base.ForegroundColor
            };
        }

        public event LabelPressHandler Clicked;

        /// <inheritdoc />
        protected override void OnCalcRect()
        {
            _clientArea = Box2i.FromDimensions(new Vector2i(), new Vector2i(
                FixedWidth == -1 ? _text.Width : FixedWidth,
                FixedHeight == -1 ? _text.Height : FixedHeight));
        }

        /// <inheritdoc />
        protected override void OnCalcPosition()
        {
            base.OnCalcPosition();

            _text.Position = _screenPos;
        }

        /// <inheritdoc />
        public override void Draw()
        {
            if (DrawTextHighlight)
                CluwneLib.drawRectangle(_text.Position.X + 3, _text.Position.Y + 4, _text.Width, _text.Height - 9, BackgroundColor);
                
            _text.Draw();

            base.Draw();
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            _text = null;
            Clicked = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (base.MouseDown(e))
                return true;

            if (ClientArea.Translated(Position).Contains(e.X, e.Y))
            {
                Clicked?.Invoke(this, e);
                return true;
            }
            return false;
        }
    }
}
