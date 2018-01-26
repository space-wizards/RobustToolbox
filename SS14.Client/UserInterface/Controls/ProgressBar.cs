using SS14.Client.Graphics;
using SS14.Client.ResourceManagement;
using SS14.Shared.Maths;
using System;
using SS14.Client.Graphics.Input;
using SS14.Client.Graphics.Sprites;
using SS14.Client.UserInterface.Controls;

namespace SS14.Client.UserInterface.Components
{
    internal class Progress_Bar : Control
    {
        public Color backgroundColor = new Color(70, 130, 180, 255);
        public Color barColor = new Color(176, 196, 222, 255);
        public Color borderColor = Color.Black;

        protected float max = 1000;
        protected float min = 0;

        protected float percent = 0;
        protected float val = 0;

        public Progress_Bar(Vector2i size)
        {
            Text = new TextSprite("", _resourceCache.GetResource<FontResource>(@"Fonts/CALIBRI.TTF").Font);
            Text.FillColor = Color.Black;
            Text.ShadowColor = new Color(105, 105, 105, 255);
            Text.ShadowOffset = new Vector2(1, 1);
            Text.Shadowed = true;

            Size = size;

            Update(0);
        }

        public TextSprite Text { get; protected set; }

        public virtual float Value
        {
            get { return val; }
            set { val = Math.Min(Math.Max(value, min), max); }
        }


        /// <inheritdoc />
        protected override void OnCalcRect()
        {
        }

        public override void Update(float frameTime)
        {
            Text.Text = Math.Round(percent*100).ToString() + "%";
            Text.Position = new Vector2i(Position.X + (int)(Size.X/2f - Text.Width/2f),
                                         Position.Y + (int)(Size.Y/2f - Text.Height/2f));
            ClientArea = Box2i.FromDimensions(Position, Size);
            Value++;
        }

        public override void Draw()
        {
            percent = (val - min)/(max - min);
            float barWidth = Size.X*percent;

            CluwneLib.drawRectangle(ClientArea.Left, ClientArea.Top, ClientArea.Width, ClientArea.Height, backgroundColor);
            CluwneLib.drawHollowRectangle (ClientArea.Left, ClientArea.Top, ClientArea.Width,ClientArea.Height, barWidth, barColor);
            CluwneLib.drawRectangle(ClientArea.Left, ClientArea.Top, ClientArea.Width, ClientArea.Height, borderColor);

            Text.Draw();
        }

        public override void Destroy()
        {
            Text = null;
            base.Destroy();
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            return false;
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            return false;
        }
    }
}
