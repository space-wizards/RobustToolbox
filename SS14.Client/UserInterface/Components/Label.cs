using OpenTK.Graphics;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Sprite;
using SS14.Client.Graphics.Utility;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.Maths;
using System;
using SS14.Client.ResourceManagement;
using Vector2i = SS14.Shared.Maths.Vector2i;

namespace SS14.Client.UserInterface.Components
{
    internal class Label : GuiComponent
    {
        #region Delegates

        public delegate void LabelPressHandler(Label sender, MouseButtonEventArgs e);

        #endregion Delegates

        private readonly IResourceCache _resourceCache;
        public Color4 BackgroundColor = Color4.Gray;
        public Color4 BorderColor = Color4.Black;
        public float BorderWidth = 2f;
        public int FixedHeight = -1;
        public int FixedWidth = -1;
        public Color4 HighlightColor = Color4.Gray;
        public TextSprite Text;

        public Label(string text, string font, uint size, IResourceCache resourceCache)
        {
            _resourceCache = resourceCache;
            Text = new TextSprite("Label" + text, text, _resourceCache.GetResource<FontResource>(@"Fonts/CALIBRI.TTF").Font) { Color = Color4.Black };
            Update(0);
        }

        public Label(string text, string font, IResourceCache resourceCache)
        {
            _resourceCache = resourceCache;
            Text = new TextSprite("Label" + text, text, _resourceCache.GetResource<FontResource>($"Fonts/{font}.TTF").Font) { Color = Color4.Black };
            Update(0);
        }

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
        public event LabelPressHandler Clicked;

        public override void Update(float frameTime)
        {
        }

        public override void Resize()
        {
            _clientArea = Box2i.FromDimensions(new Vector2i(), 
                new Vector2i(FixedWidth == -1 ? (int)Text.Width : FixedWidth,
                    FixedHeight == -1 ? (int)Text.Height : FixedHeight));

            base.Resize();

            Text.Position = _screenPos;
        }

        public override void Render()
        {
            base.Render();

            if (DrawBackground)
                CluwneLib.drawRectangle(ClientArea.Left, ClientArea.Top, ClientArea.Width, ClientArea.Height, BackgroundColor);
            if (DrawTextHighlight)
                CluwneLib.drawRectangle((int)(Text.Position.X + 3), (int)Text.Position.Y + 4, (int)Text.Width, (int)Text.Height - 9, BackgroundColor);
            if (DrawBorder)
                CluwneLib.drawHollowRectangle(ClientArea.Left, ClientArea.Top, ClientArea.Width, ClientArea.Height, BorderWidth, BorderColor);

            Text.Draw();
        }

        public override void Dispose()
        {
            Text = null;
            Clicked = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (ClientArea.Translated(Position).Contains(e.X, e.Y))
            {
                Clicked?.Invoke(this, e);
                return true;
            }

            return base.MouseDown(e);
        }
    }
}
