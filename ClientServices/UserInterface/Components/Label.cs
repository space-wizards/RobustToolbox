using System;
using System.Drawing;
using ClientInterfaces;
using ClientInterfaces.Resource;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

namespace ClientServices.UserInterface.Components
{
    class Label : GuiComponent
    {
        private readonly IResourceManager _resourceManager;

        public TextSprite Text { get; private set; }

        public delegate void LabelPressHandler(Label sender);
        public event LabelPressHandler Clicked;

        public bool DrawBorder { get; set; }
        public bool DrawBackground { get; set; }
        public bool DrawTextHighlight { get; set; }

        public Color BorderColor = Color.Black;
        public Color BackgroundColor = Color.Gray;
        public Color HighlightColor = Color.Gray;

        public int FixedWidth = -1;
        public int FixedHeight = -1;

        public Label(string text, string font, IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;

            Text = new TextSprite("Label" + text, text, _resourceManager.GetFont(font)) {Color = Color.Black};

            Update(0);
        }

        public override void Update(float frameTime)
        {
            Text.Position = Position;
            ClientArea = new Rectangle(Position, new Size(FixedWidth == -1 ? (int)Text.Width : FixedWidth, FixedHeight == -1 ? (int)Text.Height : FixedHeight));
        }

        public override void Render()
        {
            if (DrawBackground) Gorgon.CurrentRenderTarget.FilledRectangle(ClientArea.X, ClientArea.Y, ClientArea.Width, ClientArea.Height, BackgroundColor);
            if (DrawTextHighlight) Gorgon.CurrentRenderTarget.FilledRectangle(Text.Position.X + 1, Text.Position.Y + 4, Text.Width, Text.Height - 9, BackgroundColor);
            if (DrawBorder) Gorgon.CurrentRenderTarget.Rectangle(ClientArea.X, ClientArea.Y, ClientArea.Width, ClientArea.Height, BorderColor);
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
            if (ClientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
            {
                if (Clicked != null) Clicked(this);
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
