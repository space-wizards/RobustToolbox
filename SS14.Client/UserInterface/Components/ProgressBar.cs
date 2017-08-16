using SFML.Graphics;
using SFML.System;
using SFML.Window;
using OpenTK;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Sprite;
using SS14.Client.Interfaces.Resource;
using SS14.Client.ResourceManagement;
using System;
using Vector2i = SS14.Shared.Maths.Vector2i;

namespace SS14.Client.UserInterface.Components
{
    internal class Progress_Bar : GuiComponent
    {
        private readonly IResourceCache _resourceCache;
        protected Vector2i Size;

        public SFML.Graphics.Color backgroundColor = new SFML.Graphics.Color(70, 130, 180);
        public SFML.Graphics.Color barColor = new SFML.Graphics.Color(176, 196, 222);
        public SFML.Graphics.Color borderColor = Color.Black;

        protected float max = 1000;
        protected float min = 0;

        protected float percent = 0;
        protected float val = 0;

        public Progress_Bar(Vector2i size, IResourceCache resourceCache)
        {
            _resourceCache = resourceCache;
            Text = new TextSprite("ProgressBarText", "", _resourceCache.GetResource<FontResource>(@"Fonts/CALIBRI.TTF").Font);
            Text.Color = Color.Black;
            Text.ShadowColor = new SFML.Graphics.Color(105, 105, 105);
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

        public override void Update(float frameTime)
        {
            Text.Text = Math.Round(percent*100).ToString() + "%";
            Text.Position = new Vector2i(Position.X + (int)(Size.X/2f - Text.Width/2f),
                                         Position.Y + (int)(Size.Y/2f - Text.Height/2f));
            ClientArea = new IntRect(Position, Size);
            Value++;
        }

        public override void Render()
        {
            percent = (val - min)/(max - min);
            float barWidth = Size.X*percent;

            CluwneLib.drawRectangle(ClientArea.Left, ClientArea.Top, ClientArea.Width, ClientArea.Height, backgroundColor);
            CluwneLib.drawHollowRectangle (ClientArea.Left, ClientArea.Top, ClientArea.Width,ClientArea.Height, barWidth, barColor);
            CluwneLib.drawRectangle(ClientArea.Left, ClientArea.Top, ClientArea.Width, ClientArea.Height, borderColor);

            Text.Draw();
        }

        public override void Dispose()
        {
            Text = null;
            base.Dispose();
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
