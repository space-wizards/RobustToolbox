using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using GorgonLibrary.GUI;
using SS3D.UserInterface;
using Lidgren.Network;
using SS3D_shared;
using SS3D.UserInterface;

namespace SS3D.UserInterface
{
    class Window : ScrollableContainer
    {
        private Label title;
        private Rectangle titleArea;
        private GradientBox gradient;

        private bool dragging = false;
        private Vector2D draggingOffset = new Vector2D();

        private const int titleBuffer = 2;

        public Color TitleColor1 = Color.SlateGray;
        public Color TitleColor2 = Color.DarkSlateGray;

        private SimpleImageButton closeButton;
        public Boolean closeButtonVisible = true;

        public Window(string windowTitle, Size _size)
            : base(windowTitle, _size)
        {
            closeButton = new SimpleImageButton("closewindow");
            closeButton.Clicked += new SimpleImageButton.SimpleImageButtonPressHandler(closeButton_Clicked);
            title = new Label(windowTitle);
            gradient = new GradientBox();
            DrawBackground = true;
            Update();
        }

        void closeButton_Clicked(SimpleImageButton sender)
        {
            this.Dispose();
        }

        public override void Update()
        {
            if (disposing || !IsVisible()) return;
            base.Update();
            if (title == null || gradient == null) return;
            int y_pos = ClientArea.Top - (2 * titleBuffer) - title.ClientArea.Height + 1;
            title.Position = new Point(ClientArea.X + 3, y_pos + titleBuffer);
            titleArea = new Rectangle(ClientArea.X, y_pos, ClientArea.Width, title.ClientArea.Height + (2 * titleBuffer));
            title.Update();
            closeButton.Position = new Point(titleArea.Right - 5 - closeButton.ClientArea.Width, titleArea.Y + (int)(titleArea.Height / 2f) - (int)(closeButton.ClientArea.Height / 2f));
            Rectangle gradientArea = titleArea;
            gradient.ClientArea = gradientArea;
            gradient.Color1 = TitleColor1;
            gradient.Color2 = TitleColor2;
            gradient.Update();
            closeButton.Update();
        }

        public override void Render()
        {
            if (disposing || !IsVisible()) return;
            gradient.Render();
            Gorgon.Screen.Rectangle(titleArea.X, titleArea.Y, titleArea.Width, titleArea.Height, Color.Black);
            base.Render();
            title.Render();
            if(closeButtonVisible) closeButton.Render();
        }

        public override void Dispose()
        {
            if (disposing) return;
            base.Dispose();
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (disposing || !IsVisible()) return false;

            if (closeButton.MouseDown(e)) return true;

            if (base.MouseDown(e)) return true;

            if (titleArea.Contains((int)e.Position.X, (int)e.Position.Y))
            {
                draggingOffset.X = (int)e.Position.X - Position.X;
                draggingOffset.Y = (int)e.Position.Y - Position.Y;
                dragging = true;
                return true;
            }

            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            if (dragging) dragging = false;
            if (disposing || !IsVisible()) return false;
            if (base.MouseUp(e)) return true;

            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            if (disposing || !IsVisible()) return;
            if (dragging)
            {
                Position = new Point((int)e.Position.X - (int)draggingOffset.X, (int)e.Position.Y - (int)draggingOffset.Y);
            }
            base.MouseMove(e);

            return;
        }

        public override bool MouseWheelMove(MouseInputEventArgs e)
        {
            if (base.MouseWheelMove(e)) return true;
            return false;
        }

        public override bool KeyDown(KeyboardInputEventArgs e)
        {
            if (base.KeyDown(e)) return true;
            return false;
        }
    }

    public class GradientBox : GuiComponent
    {
        public Color Color1 = Color.SlateGray;
        public Color Color2 = Color.DarkSlateGray;
        VertexTypeList.PositionDiffuse2DTexture1[] box = new VertexTypeList.PositionDiffuse2DTexture1[4];
        public bool Vertical = true;

        public override void Update()
        {
            box[0].Position.X = clientArea.Left;
            box[0].Position.Y = clientArea.Top;
            box[0].TextureCoordinates.X = 0.0f;
            box[0].TextureCoordinates.Y = 0.0f;
            box[0].Color = Color1;

            box[1].Position.X = clientArea.Right;
            box[1].Position.Y = clientArea.Top;
            box[1].TextureCoordinates.X = 0.0f;
            box[1].TextureCoordinates.Y = 0.0f;
            if (!Vertical) box[1].Color = Color2;
            else box[1].Color = Color1;

            box[2].Position.X = clientArea.Right;
            box[2].Position.Y = clientArea.Bottom;
            box[2].TextureCoordinates.X = 0.0f;
            box[2].TextureCoordinates.Y = 0.0f;
            box[2].Color = Color2;

            box[3].Position.X = clientArea.Left;
            box[3].Position.Y = clientArea.Bottom;
            box[3].TextureCoordinates.X = 0.0f;
            box[3].TextureCoordinates.Y = 0.0f;
            if (!Vertical) box[3].Color = Color1;
            else box[3].Color = Color2;
        }

        public override void Render()
        {
            Gorgon.Screen.FilledRectangle(clientArea.X, clientArea.Y, clientArea.Width, clientArea.Height, Color.White); //Not sure why this is needed.
            Gorgon.Screen.Draw(box);
        }
    }
}
