using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using SS14.Client.Interfaces.Resource;
using System;
using System.Drawing;

namespace SS14.Client.Services.UserInterface.Components
{
    internal class Label : GuiComponent
    {
        #region Delegates

        public delegate void LabelPressHandler(Label sender, MouseInputEventArgs e);

        #endregion

        private readonly IResourceManager _resourceManager;
        public Color BackgroundColor = Color.Gray;
        public Color BorderColor = Color.Black;
        public int FixedHeight = -1;
        public int FixedWidth = -1;
        public Color HighlightColor = Color.Gray;

        public Label(string text, string font, IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;

            Text = new TextSprite("Label" + text, text, _resourceManager.GetFont(font)) {Color = Color.Black};

            Update(0);
        }

        public TextSprite Text { get; private set; }

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
                Gorgon.CurrentRenderTarget.FilledRectangle(ClientArea.X, ClientArea.Y, ClientArea.Width,
                                                           ClientArea.Height, BackgroundColor);
            if (DrawTextHighlight)
                Gorgon.CurrentRenderTarget.FilledRectangle(Text.Position.X + 1, Text.Position.Y + 4, Text.Width,
                                                           Text.Height - 9, BackgroundColor);
            if (DrawBorder)
                Gorgon.CurrentRenderTarget.Rectangle(ClientArea.X, ClientArea.Y, ClientArea.Width, ClientArea.Height,
                                                     BorderColor);
            Text.Draw();
        }

        public override void Dispose()
        {
            Text = null;
            Clicked = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.Position.X, (int) e.Position.Y)))
            {
                if (Clicked != null) Clicked(this, e);
                return true;
            }
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            return false;
        }
    }
}