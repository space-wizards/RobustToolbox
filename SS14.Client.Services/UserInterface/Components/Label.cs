using SS14.Client.Interfaces.Resource;
using SS14.Client.Graphics.Sprite;
using SS14.Client.Graphics;
using System;
using System.Drawing;
using SFML.Window;
using SFML.Graphics;
using Color = SFML.Graphics.Color;

namespace SS14.Client.Services.UserInterface.Components
{
    internal class Label : GuiComponent
    {
        #region Delegates

        public delegate void LabelPressHandler(Label sender, MouseButtonEventArgs e);

        #endregion

        private readonly IResourceManager _resourceManager;
        public Color BackgroundColor = new Color(128, 128, 128);
        public Color BorderColor = Color.Black;
        public float BorderWidth = 2f;
        public int FixedHeight = -1;
        public int FixedWidth = -1;
        public Color HighlightColor = new Color(128, 128, 128);
        public TextSprite Text;


        public Label(string text, string font, IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;

            Text = new TextSprite("Label" + text, text, _resourceManager.GetFont(font)) {Color = Color.Black};

            Update(0);
        }

        public Color TextColor 
        {
            get { return Text.Color; }
            set { Text.Color = value;}
        }

        public bool DrawBorder { get; set; }
        public bool DrawBackground { get; set; }
        public bool DrawTextHighlight { get; set; }
        public event LabelPressHandler Clicked;

        public override void Update(float frameTime)
        {
            Text.Position = Position;
            ClientArea = new Rectangle(Position,
                                       new Size(FixedWidth == -1 ? (int) Text.Width : FixedWidth,
                                                FixedHeight == -1 ? (int) Text.Height : FixedHeight));
        }

       
        public override void Render()
        {
            if (DrawBackground)
              CluwneLib.drawRectangle(ClientArea.X, ClientArea.Y, ClientArea.Width,ClientArea.Height, BackgroundColor);
            if (DrawTextHighlight)
                CluwneLib.drawRectangle((int)(Text.Position.X + 3), (int)Text.Position.Y + 4, (int)Text.Width, (int)Text.Height - 9, BackgroundColor);
            if (DrawBorder)
               CluwneLib.drawHollowRectangle(ClientArea.X, ClientArea.Y, ClientArea.Width, ClientArea.Height, BorderWidth, BorderColor);
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
            if (ClientArea.Contains(new Point( e.X , e.Y)))
            {
                if (Clicked != null) Clicked(this, e);
                return true;
            }
            return false;
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            return false;
        }

        public Size Size { get; set; }
    }
}