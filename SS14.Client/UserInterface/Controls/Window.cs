using OpenTK.Graphics;
using SS14.Client.Graphics.Input;
using SS14.Client.Graphics.VertexData;
using SS14.Client.Interfaces.Resource;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Maths;
using Vector2 = SS14.Shared.Maths.Vector2;
using Vector2i = SS14.Shared.Maths.Vector2i;

namespace SS14.Client.UserInterface.Components
{
    internal class Window : ScrollableContainer
    {
        protected const int titleBuffer = 1;

        public Color TitleColor1 = new Color(112, 128, 144);
        public Color TitleColor2 = new Color(47, 79, 79);

        protected ImageButton closeButton;
        public bool closeButtonVisible = true;
        protected bool dragging = false;
        protected Vector2 draggingOffset = new Vector2();
        protected GradientBox gradient;
        protected Label title;
        protected Box2i titleArea;

        public Window(string windowTitle, Vector2i size)
            : base(windowTitle, size)
        {
            closeButton = new ImageButton
            {
                ImageNormal = "closewindow"
            };

            closeButton.Clicked += CloseButtonClicked;
            title = new Label(windowTitle, "CALIBRI");
            gradient = new GradientBox();

            BackgroundColor = new Color4(169, 169, 169, 255);
            BorderColor = Color4.Black;
            DrawBackground = true;
            DrawBorder = true;
            BorderWidth = 1;

            Update(0);
        }

        virtual protected void CloseButtonClicked(ImageButton sender)
        {
            Dispose();
        }

        public override void DoLayout()
        {
            base.DoLayout();
        }

        public override void Update(float frameTime)
        {
            if (Disposing || !Visible) return;
            base.Update(frameTime);
            if (title == null || gradient == null) return;
            
            int y_pos = ClientArea.Top - (2 * titleBuffer) - title.ClientArea.Height + 1;
            title.LocalPosition = Position + new Vector2i(ClientArea.Left + 3, y_pos + titleBuffer);
            titleArea = Box2i.FromDimensions(ClientArea.Left, y_pos, ClientArea.Width, title.ClientArea.Height + (2 * titleBuffer));
            title.DoLayout();
            title.Update(frameTime);

            closeButton.LocalPosition = Position + new Vector2i(titleArea.Right - 5 - closeButton.ClientArea.Width,
                                             titleArea.Top + (int)(titleArea.Height / 2f) -
                                             (int)(closeButton.ClientArea.Height / 2f));
            closeButton.DoLayout();
            gradient.ClientArea = titleArea;
            gradient.Color1 = TitleColor1;
            gradient.Color2 = TitleColor2;
            gradient.Update(frameTime);
            closeButton.Update(frameTime);
        }

        public override void Draw() // Renders the main window
        {
            if (Disposing || !Visible) return;
            gradient.Draw();

            //TODO RenderTargetRectangle
            base.Draw();
            title.Draw();
            if (closeButtonVisible) closeButton.Draw();
        }

        public override void Dispose()
        {
            if (Disposing) return;
            base.Dispose();
        }

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (Disposing || !IsVisible()) return false;

            if (closeButton.MouseDown(e)) return true;

            if (base.MouseDown(e)) return true;

            if (titleArea.Contains((int)e.X, (int)e.Y))
            {
                draggingOffset = new Vector2(e.X, e.Y) - Position;
                dragging = true;
                return true;
            }

            return false;
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            if (dragging) dragging = false;
            if (Disposing || !IsVisible()) return false;
            if (base.MouseUp(e)) return true;

            return false;
        }

        public override void MouseMove(MouseMoveEventArgs e)
        {
            if (Disposing || !IsVisible()) return;
            if (dragging)
            {
                Position = new Vector2i((int)e.X - (int)draggingOffset.X,
                                     (int)e.Y - (int)draggingOffset.Y);
            }
            base.MouseMove(e);

            return;
        }

        public override bool MouseWheelMove(MouseWheelScrollEventArgs e)
        {
            if (base.MouseWheelMove(e)) return true;
            return false;
        }

        public override bool KeyDown(KeyEventArgs e)
        {
            if (base.KeyDown(e)) return true;
            return false;
        }
    }

    public class GradientBox : Control
    {
        private readonly VertexTypeList.PositionDiffuse2DTexture1[] box =
            new VertexTypeList.PositionDiffuse2DTexture1[4];

        public Color Color1 = new Color(112, 128, 144);
        public Color Color2 = new Color(47, 79, 79);

        public bool Vertical = true;

        public override void Update(float frameTime)
        {
            box[0].Position.X = ClientArea.Left;
            box[0].Position.Y = ClientArea.Top;
            box[0].TextureCoordinates = Vector2.Zero;
            box[0].Color = Color1;

            box[1].Position.X = ClientArea.Right;
            box[1].Position.Y = ClientArea.Top;
            box[1].TextureCoordinates = Vector2.Zero;
            if (!Vertical) box[1].Color = Color2;
            else box[1].Color = Color1;

            box[2].Position.X = ClientArea.Right;
            box[2].Position.Y = ClientArea.Bottom;
            box[2].TextureCoordinates = Vector2.Zero;
            box[2].Color = Color2;

            box[3].Position.X = ClientArea.Left;
            box[3].Position.Y = ClientArea.Bottom;
            box[3].TextureCoordinates = Vector2.Zero;
            if (!Vertical) box[3].Color = Color1;
            else box[3].Color = Color2;
        }

        protected override void OnCalcRect()
        {
            throw new System.NotImplementedException();
        }

        public override void Draw()
        {
            //TODO Window Render
        }
    }
}
