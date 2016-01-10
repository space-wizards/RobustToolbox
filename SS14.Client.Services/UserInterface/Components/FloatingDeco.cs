using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared.IoC;
using System;

namespace SS14.Client.Services.UserInterface.Components
{
    internal class FloatingDecoration : GuiComponent
    {
        private readonly IResourceManager _resourceManager;
        private readonly UserInterfaceManager _uiMgr;
        public bool BounceRotate = false; //Rotation inverts after hitting a certain angle?
        public float BounceRotateAngle = 0; //Angle at which to change rotation direction.

        public Sprite DrawSprite;

        public bool MouseParallax = true; //Move with mouse?
        public bool MouseParallaxHorizontal = true;
        public bool MouseParallaxVertical = true;
        private Vector2f ParallaxOffset;

        public float ParallaxScale = 0.01f; //Mouse Parallax Rate Modifier.
        public float RotationSpeed = 0; //Speed and direction at which this rotates.

        public Vector2f SpriteLocation;
        //Have to have a separate one because i made the ui compo pos a Point. Can't change to Vector2 unless i fix 235+ errors. Do this later.

        public Vector2f Velocity; //Direction and speed this is moving in.

        private float spriteRotation;

        public FloatingDecoration(IResourceManager resourceManager, string spriteName)
        {
            _resourceManager = resourceManager;
            DrawSprite = _resourceManager.GetSprite(spriteName);
//DrawSprite.Smoothing = Smoothing.Smooth;

            _uiMgr = (UserInterfaceManager) IoCManager.Resolve<IUserInterfaceManager>();

            Update(0);
        }

        public override void Update(float frameTime)
        {
            SpriteLocation = new Vector2f(SpriteLocation.X + (Velocity.X*frameTime),
                                          SpriteLocation.Y + (Velocity.Y*frameTime));
            spriteRotation += RotationSpeed*frameTime;

            if (BounceRotate && Math.Abs(spriteRotation) > BounceRotateAngle)
                RotationSpeed = -RotationSpeed;

            var bounds = DrawSprite.GetLocalBounds();

            ClientArea = new IntRect((int) SpriteLocation.X, (int) SpriteLocation.Y,
                                       (int)bounds.Width, (int)bounds.Height);

            //Outside screen. Does not respect rotation. FIX.
            if (ClientArea.Left >CluwneLib.Screen.Size.X)
                SpriteLocation = new Vector2f((0 - bounds.Width), SpriteLocation.Y);
            else if (ClientArea.Left < (0 - bounds.Width))
                SpriteLocation = new Vector2f(CluwneLib.Screen.Size.X, SpriteLocation.Y);

            if (ClientArea.Top > CluwneLib.Screen.Size.Y)
                SpriteLocation = new Vector2f(SpriteLocation.X, (0 - bounds.Height));
            else if (ClientArea.Top < (0 - bounds.Height))
                SpriteLocation = new Vector2f(SpriteLocation.X, CluwneLib.Screen.Size.Y);

            if (MouseParallax)
            {
                float ParX = 0;
                float ParY = 0;

                if (MouseParallaxHorizontal)
                {
                    ParX = Math.Abs(_uiMgr.MousePos.X - (CluwneLib.Screen.Size.X));
                    ParX *= ParallaxScale;
                }

                if (MouseParallaxVertical)
                {
                    ParY = Math.Abs(_uiMgr.MousePos.Y - ((CluwneLib.Screen.Size.Y)));
                    ParY *= ParallaxScale;
                }

                ParallaxOffset = new Vector2f(ParX, ParY);
            }
            else
            {
                ParallaxOffset = new Vector2f();
            }

            Position = new Vector2i((int) SpriteLocation.X, (int) SpriteLocation.Y);
        }

        public override void Render()
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