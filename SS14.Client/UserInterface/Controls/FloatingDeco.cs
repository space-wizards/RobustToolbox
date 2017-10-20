using OpenTK;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Utility;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using SS14.Shared.Maths;
using System;
using Vector2i = SS14.Shared.Maths.Vector2i;
using Vector2 = SS14.Shared.Maths.Vector2;
using SS14.Client.Graphics.Input;
using SS14.Client.Graphics.Sprites;

namespace SS14.Client.UserInterface.Components
{
    internal class FloatingDecoration : Control
    {
        private readonly IResourceCache _resourceCache;
        private readonly UserInterfaceManager _uiMgr;
        public bool BounceRotate = false; //Rotation inverts after hitting a certain angle?
        public float BounceRotateAngle = 0; //Angle at which to change rotation direction.

        public Sprite DrawSprite;

        public bool MouseParallax = true; //Move with mouse?
        public bool MouseParallaxHorizontal = true;
        public bool MouseParallaxVertical = true;
        private Vector2 ParallaxOffset;

        public float ParallaxScale = 0.01f; //Mouse Parallax Rate Modifier.
        public float RotationSpeed = 0; //Speed and direction at which this rotates.

        public Vector2 SpriteLocation;
        //Have to have a separate one because i made the ui compo pos a Point. Can't change to Vector2 unless i fix 235+ errors. Do this later.

#pragma warning disable CS0649
        public Vector2 Velocity; //Direction and speed this is moving in.
#pragma warning restore CS0649

        private float spriteRotation;

        public FloatingDecoration(IResourceCache resourceCache, string spriteName)
        {
            _resourceCache = resourceCache;
            DrawSprite = _resourceCache.GetSprite(spriteName);

            _uiMgr = (UserInterfaceManager)IoCManager.Resolve<IUserInterfaceManager>();

            Update(0);
        }

        /// <inheritdoc />
        protected override void OnCalcRect()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        protected override void OnCalcPosition()
        {
            base.OnCalcPosition();
        }


        public override void Update(float frameTime)
        {
            SpriteLocation = new Vector2(SpriteLocation.X + (Velocity.X * frameTime),
                                          SpriteLocation.Y + (Velocity.Y * frameTime));
            spriteRotation += RotationSpeed * frameTime;

            if (BounceRotate && Math.Abs(spriteRotation) > BounceRotateAngle)
                RotationSpeed = -RotationSpeed;

            var bounds = DrawSprite.LocalBounds;

            ClientArea = Box2i.FromDimensions((int)SpriteLocation.X, (int)SpriteLocation.Y,
                                       (int)bounds.Width, (int)bounds.Height);

            //Outside screen. Does not respect rotation. FIX.
            if (ClientArea.Left > CluwneLib.Window.Viewport.Size.X)
                SpriteLocation = new Vector2((0 - bounds.Width), SpriteLocation.Y);
            else if (ClientArea.Left < (0 - bounds.Width))
                SpriteLocation = new Vector2(CluwneLib.Window.Viewport.Size.X, SpriteLocation.Y);

            if (ClientArea.Top > CluwneLib.Window.Viewport.Size.Y)
                SpriteLocation = new Vector2(SpriteLocation.X, (0 - bounds.Height));
            else if (ClientArea.Top < (0 - bounds.Height))
                SpriteLocation = new Vector2(SpriteLocation.X, CluwneLib.Window.Viewport.Size.Y);

            if (MouseParallax)
            {
                float ParX = 0;
                float ParY = 0;

                if (MouseParallaxHorizontal)
                {
                    ParX = Math.Abs(_uiMgr.MousePos.X - (CluwneLib.Window.Viewport.Size.X));
                    ParX *= ParallaxScale;
                }

                if (MouseParallaxVertical)
                {
                    ParY = Math.Abs(_uiMgr.MousePos.Y - ((CluwneLib.Window.Viewport.Size.Y)));
                    ParY *= ParallaxScale;
                }

                ParallaxOffset = new Vector2(ParX, ParY);
            }
            else
            {
                ParallaxOffset = new Vector2();
            }

            Position = new Vector2i((int)SpriteLocation.X, (int)SpriteLocation.Y);
        }

        public override void Draw()
        {
            DrawSprite.Rotation = spriteRotation;
            DrawSprite.Position = (SpriteLocation + ParallaxOffset);
            DrawSprite.Draw();
        }

        public override void Dispose()
        {
            DrawSprite = null;
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
