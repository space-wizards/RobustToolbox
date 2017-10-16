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
    internal class Label : GuiComponent
    {
        public delegate void LabelPressHandler(Label sender, MouseButtonEventArgs e);
        
        public Color4 BackgroundColor = Color4.Gray;
        public Color4 BorderColor = Color4.Black;
        public float BorderWidth = 2f;
        public int FixedHeight = -1;
        public int FixedWidth = -1;
        public Color4 HighlightColor = Color4.Gray;
        public TextSprite Text;

        public uint FontSize
        {
            get => Text.FontSize;
            set => Text.FontSize = value;
        }

        public Color4 TextColor
        {
            get => Text.Color;
            set => Text.Color = value;
        }

        public bool DrawBorder { get; set; }
        public bool DrawBackground { get; set; }
        public bool DrawTextHighlight { get; set; }

        public Label(string text, string font, uint size, IResourceCache resourceCache)
        {
            Text = new TextSprite(text, resourceCache.GetResource<FontResource>($"Fonts/{font}.TTF").Font, size) {Color = Color4.Black};
        }

        public Label(string text, string font, IResourceCache resourceCache)
        {
            Text = new TextSprite(text, resourceCache.GetResource<FontResource>($"Fonts/{font}.TTF").Font) {Color = Color4.Black};
        }

        public event LabelPressHandler Clicked;

        /// <inheritdoc />
        protected override void OnCalcRect()
        {
            _clientArea = Box2i.FromDimensions(new Vector2i(), new Vector2i(
                FixedWidth == -1 ? Text.Width : FixedWidth,
                FixedHeight == -1 ? Text.Height : FixedHeight));
        }

        /// <inheritdoc />
        protected override void OnCalcPosition()
        {
            base.OnCalcPosition();

            Text.Position = _screenPos;
        }

        /// <inheritdoc />
        public override void Render()
        {
            if (DrawBackground)
                CluwneLib.drawRectangle(ClientArea.Left, ClientArea.Top, ClientArea.Width, ClientArea.Height, BackgroundColor);
            if (DrawTextHighlight)
                CluwneLib.drawRectangle(Text.Position.X + 3, Text.Position.Y + 4, Text.Width, Text.Height - 9, BackgroundColor);
            if (DrawBorder)
                CluwneLib.drawHollowRectangle(ClientArea.Left, ClientArea.Top, ClientArea.Width, ClientArea.Height, BorderWidth, BorderColor);

            Text.Draw();

            base.Render();
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            Text = null;
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
