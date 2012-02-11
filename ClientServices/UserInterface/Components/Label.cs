using System;
using System.Drawing;
using ClientInterfaces;
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

        public bool DrawBorder;
        public bool DrawBackground;

        public Color BorderColor = Color.Black;
        public Color BackgroundColor = Color.Gray;

        public int FixedWidth = -1;
        public int FixedHeight = -1;

        public Label(string text, string font, IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;

            Text = new TextSprite("Label" + text, text, _resourceManager.GetFont(font)) {Color = Color.Black};

            Update();
        }

        public override void Update()
        {
            Text.Position = Position;
            ClientArea = new Rectangle(Position, new Size(FixedWidth == -1 ? (int)Text.Width : FixedWidth, FixedHeight == -1 ? (int)Text.Height : FixedHeight));
        }

        public override void Render()
        {
            if (DrawBackground) Gorgon.Screen.FilledRectangle(ClientArea.X, ClientArea.Y, ClientArea.Width, ClientArea.Height, BackgroundColor);
            if (DrawBorder) Gorgon.Screen.Rectangle(ClientArea.X, ClientArea.Y, ClientArea.Width, ClientArea.Height, BorderColor);
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
