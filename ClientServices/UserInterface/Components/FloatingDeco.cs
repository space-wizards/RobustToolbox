using System;
using System.Drawing;
using ClientInterfaces.Resource;
using ClientInterfaces.UserInterface;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using SS13.IoC;

namespace ClientServices.UserInterface.Components
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
        private Vector2D ParallaxOffset;

        public float ParallaxScale = 0.01f; //Mouse Parallax Rate Modifier.
        public float RotationSpeed = 0; //Speed and direction at which this rotates.

        public Vector2D SpriteLocation;
                        //Have to have a separate one because i made the ui compo pos a Point. Can't change to Vector2d unless i fix 235+ errors. Do this later.

        public Vector2D Velocity; //Direction and speed this is moving in.

        private float spriteRotation;

        public FloatingDecoration(IResourceManager resourceManager, string spriteName)
        {
            _resourceManager = resourceManager;
            DrawSprite = _resourceManager.GetSprite(spriteName);
            DrawSprite.Smoothing = Smoothing.Smooth;

            _uiMgr = (UserInterfaceManager) IoCManager.Resolve<IUserInterfaceManager>();

            Update(0);
        }

        public override void Update(float frameTime)
        {
            SpriteLocation = new Vector2D(SpriteLocation.X + (Velocity.X*frameTime),
                                          SpriteLocation.Y + (Velocity.Y*frameTime));
            spriteRotation += RotationSpeed*frameTime;

            if (BounceRotate && Math.Abs(spriteRotation) > BounceRotateAngle)
                RotationSpeed = -RotationSpeed;

            ClientArea = new Rectangle(new Point((int) SpriteLocation.X, (int) SpriteLocation.Y),
                                       new Size((int) DrawSprite.Width, (int) DrawSprite.Height));

            //Outside screen. Does not respect rotation. FIX.
            if (ClientArea.X > Gorgon.Screen.Width)
                SpriteLocation = new Vector2D((0 - DrawSprite.Width), SpriteLocation.Y);
            else if (ClientArea.X < (0 - DrawSprite.Width))
                SpriteLocation = new Vector2D(Gorgon.Screen.Width, SpriteLocation.Y);

            if (ClientArea.Y > Gorgon.Screen.Height)
                SpriteLocation = new Vector2D(SpriteLocation.X, (0 - DrawSprite.Height));
            else if (ClientArea.Y < (0 - DrawSprite.Height))
                SpriteLocation = new Vector2D(SpriteLocation.X, Gorgon.Screen.Height);

            if (MouseParallax)
            {
                float ParX = 0;
                float ParY = 0;

                if (MouseParallaxHorizontal)
                {
                    ParX = Math.Abs(_uiMgr.MousePos.X - (Gorgon.Screen.Width));
                    ParX *= ParallaxScale;
                }

                if (MouseParallaxVertical)
                {
                    ParY = Math.Abs(_uiMgr.MousePos.Y - (Gorgon.Screen.Height));
                    ParY *= ParallaxScale;
                }

                ParallaxOffset = new Vector2D(ParX, ParY);
            }
            else
            {
                ParallaxOffset = Vector2D.Zero;
            }

            Position = new Point((int) SpriteLocation.X, (int) SpriteLocation.Y);
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

        public override bool MouseDown(MouseInputEventArgs e)
        {
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            return false;
        }
    }
}