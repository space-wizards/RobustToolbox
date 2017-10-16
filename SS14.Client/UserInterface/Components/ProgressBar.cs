using SFML.Graphics;
using SFML.System;
using SFML.Window;
using OpenTK;
using OpenTK.Graphics;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Sprite;
using SS14.Client.Interfaces.Resource;
using SS14.Client.ResourceManagement;
using SS14.Shared.Maths;
using System;
using Vector2i = SS14.Shared.Maths.Vector2i;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Client.UserInterface.Components
{
    internal class Progress_Bar : GuiComponent
    {
        private readonly IResourceCache _resourceCache;
        protected Vector2i Size;

        public Color4 backgroundColor = new Color4(70, 130, 180, 255);
        public Color4 barColor = new Color4(176, 196, 222, 255);
        public Color4 borderColor = Color4.Black;

        protected float max = 1000;
        protected float min = 0;

        protected float percent = 0;
        protected float val = 0;

        public Progress_Bar(Vector2i size, IResourceCache resourceCache)
        {
            _resourceCache = resourceCache;
            Text = new TextSprite("", _resourceCache.GetResource<FontResource>(@"Fonts/CALIBRI.TTF").Font);
            Text.Color = Color4.Black;
            Text.ShadowColor = new Color4(105, 105, 105, 255);
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
