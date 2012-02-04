using System;
using System.Drawing;
using System.Text;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using ClientResourceManager;

namespace CGO
{
    public class SpeechBubble
    {
        #region Fields

        /// <summary>
        /// Constant lifetime of speech bubble in milliseconds.
        /// TODO: Consider making configurable?
        /// </summary>
        private const double MillisecondsToLive = 8000;

        /// <summary>
        /// Owner mob unique name.
        /// </summary>
        private readonly string _mobName;

        /// <summary>
        /// TextSprite used to hold and display speech text.
        /// </summary>
        private readonly TextSprite _textSprite;

        /// <summary>
        /// StringBuilder to handle 
        /// </summary>
        private readonly StringBuilder _stringBuilder;

        /// <summary>
        /// Holder for built bubble render image.
        /// Rebuilt upon text change.
        /// </summary>
        private readonly RenderImage _bubbleRender;

        /// <summary>
        /// Holder for built bubble sprite.
        /// Rebuilt upon text change.
        /// </summary>
        private readonly Sprite _bubbleSprite;
        
        /// <summary>
        /// Holder for last time the sprite was
        /// built. Used to detect if speech bubble
        /// has expired.
        /// </summary>
        private DateTime _buildTime;

        #endregion

        #region Methods

        #region Constructor

        public SpeechBubble(string mobname)
        {
            _mobName = mobname;
            _buildTime = DateTime.Now;
            _textSprite = new TextSprite
                (
                    "chatBubbleTextSprite_" + _mobName,
                    String.Empty, 
                    ResMgr.Singleton.GetFont("CALIBRI")
                )
                              {
                                  Color = Color.Black,
                                  WordWrap = true
                              };

            _textSprite.SetPosition(5, 3);
            _stringBuilder = new StringBuilder();

            _bubbleRender = new RenderImage("ChatBubbleRenderImage_" + _mobName, 1, 1, ImageBufferFormats.BufferRGB888A8);
            _bubbleSprite = new Sprite("ChatBubbleRenderSprite_" + _mobName, _bubbleRender);
        }

        #endregion

        #region Privates

        #endregion

        #region Publics

        public void Draw(Vector2D position, Vector2D windowOrigin, Sprite spriteToDrawAbove)
        {
            if ((DateTime.Now - _buildTime).TotalMilliseconds >= MillisecondsToLive) return;

            var x = position.X - windowOrigin.X - (_bubbleSprite.Width / 2.0f);
            var y = position.Y - windowOrigin.Y - (_bubbleSprite.Height) - (spriteToDrawAbove.Height / 2.0f) - 5.0f;

            _bubbleSprite.SetPosition(x, y);
            _bubbleSprite.Draw();
        }

        public void SetText(string text)
        {
            for (var i = 0; i < text.Length; i++)
            {
                if (i > 0 && i % 50 == 0)
                    _stringBuilder.Append("\n" + text[i]);
                else
                    _stringBuilder.Append(text[i]);
            }

            _textSprite.Text = _stringBuilder.ToString();
            _stringBuilder.Clear();
            _textSprite.UpdateAABB();

            DrawBubbleSprite();
        }

        #endregion

        private void DrawBubbleSprite()
        {
            var originalTarget = Gorgon.CurrentRenderTarget;
            var cornerSprite = ResMgr.Singleton.GetSprite("corners");

            //Set up dimensions
            _bubbleRender.SetDimensions((int)_textSprite.Size.X + 10, (int)_textSprite.Size.Y + 10);
            _bubbleSprite.SetSize(_textSprite.Size.X + 10, _textSprite.Size.Y + 10);

            //BEGIN RENDERING
            Gorgon.CurrentRenderTarget = _bubbleRender;
            _bubbleRender.Clear(Color.Transparent);

            //Draw black triangle at the bottom.
            var blacktriangle = new VertexTypeList.PositionDiffuse2DTexture1[3];
            blacktriangle[0].Position.X = (_bubbleRender.Width / 2) - 10;
            blacktriangle[1].Position.X = (_bubbleRender.Width / 2) + 10;
            blacktriangle[2].Position.X = (_bubbleRender.Width / 2);
            blacktriangle[0].Position.Y = _bubbleRender.Height - 15;
            blacktriangle[1].Position.Y = _bubbleRender.Height - 15;
            blacktriangle[2].Position.Y = _bubbleRender.Height;
            blacktriangle[0].TextureCoordinates = Vector2D.Zero;
            blacktriangle[0].Color = Color.Black;
            blacktriangle[1].TextureCoordinates = Vector2D.Zero;
            blacktriangle[1].Color = Color.Black;
            blacktriangle[2].TextureCoordinates = Vector2D.Zero;
            blacktriangle[2].Color = Color.Black;
            _bubbleRender.Draw(blacktriangle);
            
            //Draw the side lines
            _bubbleRender.Line(10, 0, _bubbleRender.Width - 20, 1, Color.Black);
            _bubbleRender.Line(_bubbleRender.Width - 1, 10, 1, _bubbleRender.Height - 26, Color.Black);
            _bubbleRender.Line(10, _bubbleRender.Height - 7, _bubbleRender.Width - 20, 1, Color.Black);
            _bubbleRender.Line(0, 10, 1, _bubbleRender.Height - 26, Color.Black);

            //Fill in the middle without polluting the corners.
            _bubbleRender.FilledRectangle(3, 1, _bubbleRender.Width - 6, _bubbleRender.Height - 8, Color.White);
            _bubbleRender.FilledRectangle(1, 3, _bubbleRender.Width - 2, _bubbleRender.Height - 12, Color.White);

            //Draw the white triangle at the bottom.
            var whiteTriangle = new VertexTypeList.PositionDiffuse2DTexture1[3];
            whiteTriangle[0].Position.X = (_bubbleRender.Width / 2) - 8;
            whiteTriangle[1].Position.X = (_bubbleRender.Width / 2) + 8;
            whiteTriangle[2].Position.X = (_bubbleRender.Width / 2);
            whiteTriangle[0].Position.Y = _bubbleRender.Width - 15;
            whiteTriangle[1].Position.Y = _bubbleRender.Width - 15;
            whiteTriangle[2].Position.Y = _bubbleRender.Height - 2;
            whiteTriangle[0].TextureCoordinates = Vector2D.Zero;
            whiteTriangle[0].Color = Color.White;
            whiteTriangle[1].TextureCoordinates = Vector2D.Zero;
            whiteTriangle[1].Color = Color.White;
            whiteTriangle[2].TextureCoordinates = Vector2D.Zero;
            whiteTriangle[2].Color = Color.White;
            _bubbleRender.Draw(whiteTriangle);

            //Draw the corners.
            cornerSprite.SourceBlend = AlphaBlendOperation.One;
            cornerSprite.DestinationBlend = AlphaBlendOperation.Zero;
            cornerSprite.VerticalFlip = true;
            cornerSprite.SetPosition(0, 0);
            cornerSprite.Draw();
            cornerSprite.HorizontalFlip = true;
            cornerSprite.SetPosition(_bubbleRender.Width - 16, 0);
            cornerSprite.Draw();
            cornerSprite.VerticalFlip = false;
            cornerSprite.SetPosition(_bubbleRender.Width - 16, _bubbleRender.Height - 22);
            cornerSprite.Draw();
            cornerSprite.HorizontalFlip = false;
            cornerSprite.SetPosition(0, _bubbleRender.Height - 22);
            cornerSprite.Draw();

            _textSprite.Draw();

            Gorgon.CurrentRenderTarget = originalTarget;

            _buildTime = DateTime.Now;
        }

        #endregion

        /*MiyagiSystem mMiyagiMgr;
        OgreManager mEngine;
        MovableObject host;

        public Vector2 offset = new Vector2(0f, -40f);

        Panel mainBubble;
        Panel addBubble;
        Label Text;

        private const string defaultBubbleSkin = "ChatbubbleSkin";
        private const string defaultBubbleTailSkin = "ChatbubbleTailSkin";

        private float Opacity = 0;
        public float opacity
        {
            get
            {
                return Opacity;
            }

            private set
            {
                this.Opacity = value;
                this.mainBubble.Opacity = value;
                this.addBubble.Opacity = value;
                this.Text.Opacity = value;
            }
        }

        private DateTime removeAt;

        ~SpeechBubble()
        {
            mMiyagiMgr = null;
            mEngine = null;
            host = null;
            mainBubble = null;
            addBubble = null;
            Text = null;
        }

        public SpeechBubble(OgreManager ogremgr, MovableObject ihost)
        {
            mMiyagiMgr = ogremgr.mMiyagiSystem;
            mEngine = ogremgr;
            host = ihost;

            this.mainBubble = new Panel()
            {
                Location = new Point(0, 0),
                Size = new Size(0, 0),
                ResizeMode = ResizeModes.None,
                AutoSize = true,
                AutoSizeMode = Miyagi.UI.AutoSizeMode.GrowAndShrink,
                Padding = new Thickness(8, 15, 8, 15),
                HitTestVisible = false,
                Movable = false,
                TextureFiltering = Miyagi.Common.TextureFiltering.Anisotropic,
                Skin = MiyagiResources.Singleton.Skins[defaultBubbleSkin]
            };
            this.Controls.Add(mainBubble);

            this.addBubble = new Panel()
            {
                Location = new Point(0, 0),
                Size = new Size(11, 21),
                ResizeMode = ResizeModes.None,
                HitTestVisible = false,
                Movable = false,
                TextureFiltering = Miyagi.Common.TextureFiltering.Anisotropic,
                Skin = MiyagiResources.Singleton.Skins[defaultBubbleTailSkin]
            };
            this.Controls.Add(addBubble);

            this.Text = new Label()
            {
                Location = new Point(0, 0),
                Size = new Size(0, 0),
                AutoSize = true,
                AutoSizeMode = Miyagi.UI.AutoSizeMode.GrowAndShrink,
                Text = "",
                HitTestVisible = false,
                TextureFiltering = Miyagi.Common.TextureFiltering.Anisotropic,
                TextStyle =
                {
                    Alignment = Alignment.MiddleCenter,
                    ForegroundColour = Colours.Black
                }
            };
            mainBubble.Controls.Add(Text);

            Hide();

            this.Updating += new EventHandler(SpeechBubble_Updating);

            mMiyagiMgr.GUIManager.GUIs.Add(this);
        }

        public void Show(string text)
        {
            this.opacity = 100;
            //this.Visible = true;
            this.Text.Text = text;
            this.removeAt = DateTime.Now.AddMilliseconds(4000);
        }

        public void Show(string text, double durationMilli)
        {
            this.opacity = 100;
            //this.Visible = true;
            this.Text.Text = text;
            this.removeAt = DateTime.Now.AddMilliseconds(durationMilli);
        }

        public void Hide()
        {
            this.opacity = 0;
            //this.Visible = false;
        }

        public void ScheduleDispose() //Schedules removal after next update.
        {
            this.Updated += new EventHandler(SpeechBubble_DisposeNow);
        }

        private void SpeechBubble_DisposeNow(object sender, EventArgs e)
        {
            Dispose();
            mMiyagiMgr.GUIManager.GUIs.Remove(this);
        } //Used by ScheduleDispose()

        private void SpeechBubble_Updating(object sender, EventArgs e) //Updates position and visibility.
        {
            #region Host dispose check
            try
            {
                if (host.BoundingBox == null) return; //Since theres no good way of figuring out when the object is disposed. Ugly :(
            }
            catch (NullReferenceException)
            {
                ScheduleDispose();
                return;
            } 
            #endregion

            if ( (this.Visible && this.opacity > 0) && DateTime.Compare(DateTime.Now, removeAt) > 0)
            {
                Hide();
                return;
            }

            AxisAlignedBox objectAAB = host.GetWorldBoundingBox(true);
            Matrix4 viewMatrix = mEngine.Camera.ViewMatrix;
            Matrix4 projMatrix = mEngine.Camera.ProjectionMatrix;

            Mogre.Vector3 objPoint = (
                objectAAB.GetCorner(AxisAlignedBox.CornerEnum.FAR_LEFT_TOP) +
                objectAAB.GetCorner(AxisAlignedBox.CornerEnum.FAR_RIGHT_TOP) +
                objectAAB.GetCorner(AxisAlignedBox.CornerEnum.NEAR_LEFT_TOP) +
                objectAAB.GetCorner(AxisAlignedBox.CornerEnum.NEAR_RIGHT_TOP)
                ) / 4f; //Average of 4 TOP points of bounding box.

            if (this.Visible && this.opacity > 0)
            {
                Plane cameraPlane = new Plane(mEngine.Camera.DerivedOrientation.ZAxis, mEngine.Camera.DerivedPosition);
                Boolean isOutsideCam = (cameraPlane.GetSide(objPoint) != Plane.Side.NEGATIVE_SIDE);
                if (isOutsideCam)
                {
                    Hide();
                    return;
                }
            }

            objPoint = projMatrix * (viewMatrix * objPoint); //Transform to screen space.

            float result_x, result_y;
            result_x = (objPoint.x / 2) + 0.5f; //Normalize. (From -1 - 1 to 0 - 1)
            result_y = 1-((objPoint.y / 2) + 0.5f);

            int prop_x = (int)(result_x * (float)mEngine.Window.Width) - (int)(mainBubble.Width / 2f) + (int)offset.x;
            int prop_y = (int)(result_y * (float)mEngine.Window.Height) - (int)(mainBubble.Height / 2f) + (int)offset.y;

            this.mainBubble.Location = new Point(prop_x, prop_y);
            int tail_x = mainBubble.Location.X + (int)((mainBubble.Size.Width / 2f) - (addBubble.Size.Width / 2f));
            int tail_y = mainBubble.Location.Y + mainBubble.Size.Height - 3;
            this.addBubble.Location = new Point(tail_x, tail_y);
        }
        */
    }
}