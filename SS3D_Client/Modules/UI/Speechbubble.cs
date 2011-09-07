using System;

using Lidgren.Network;

using SS3D.Modules;
using SS3D.Modules.Map;
using SS3D.Modules.Network;
using SS3D.Modules.UI;
using SS3D.Atom;

using SS3D_shared;

using GorgonLibrary;
using GorgonLibrary.Graphics;

namespace SS3D.Modules.UI
{
    public class SpeechBubble
    {
        private RenderImage m_completeBubble;
        private Sprite m_completeBubbleSprite;
        private TextSprite textSprite;
        private string m_text;
        private string m_mobname;
        private bool spriteupdaterequired = false;
        private double millisecondstolive = 8000;
        private double millisecondsremaining = 0;
        private DateTime lastUpdate;


        public SpeechBubble(string mobname)
        {
            m_mobname = mobname;
            lastUpdate = DateTime.Now;
        }

        public void Draw(Vector2D position, float xTopLeft, float yTopLeft, Sprite spriteToDrawAbove)
        {
            if (spriteupdaterequired)
                DrawBubbleSprite();
            if (millisecondsremaining <= 0)
                return;
            millisecondsremaining -= (DateTime.Now - lastUpdate).TotalMilliseconds;

            int x = (int)Math.Round(position.X - xTopLeft - (m_completeBubbleSprite.Width / 2));
            int y = (int)Math.Round(position.Y - yTopLeft - (m_completeBubbleSprite.Height) - (spriteToDrawAbove.Height / 2) - 5);
            m_completeBubbleSprite.SetPosition(x, y);
            m_completeBubbleSprite.Draw();
            lastUpdate = DateTime.Now;
        }

        public void SetText(string text)
        {
            m_text = "";
            for (int i = 0; i < text.Length; i++)
            {
                if (i > 0 && i % 50 == 0)
                    m_text += "\n" + text.Substring(i, 1);
                else
                    m_text += text.Substring(i, 1);
            }
            spriteupdaterequired = true;
        }

        public void DrawBubbleSprite()
        {
            var target = Gorgon.CurrentRenderTarget; // store current rendertarget
            Sprite cornersprite = ResMgr.Singleton.GetSprite("corners");

            //Set up dimensions

            if (textSprite == null)
            {
                textSprite = new TextSprite("chatBubbleTextSprite_" + m_mobname, "", ResMgr.Singleton.GetFont("CALIBRI"));
                textSprite.Color = System.Drawing.Color.Black;
                textSprite.WordWrap = true;
                textSprite.SetPosition(5, 3);
            }

            textSprite.Text = m_text;
            textSprite.UpdateAABB();

            if (m_completeBubble == null)
                m_completeBubble = new RenderImage("ChatBubbleRenderImage_" + m_mobname, 1, 1, ImageBufferFormats.BufferRGB888A8);
            if (m_completeBubbleSprite == null)
                m_completeBubbleSprite = new Sprite("ChatBubbleRenderSprite_" + m_mobname, m_completeBubble);

            m_completeBubble.SetDimensions((int)textSprite.Size.X + 10, (int)textSprite.Size.Y + 10);
            m_completeBubbleSprite.SetSize(textSprite.Size.X + 10, textSprite.Size.Y + 10);

            //BEGIN RENDERING
            Gorgon.CurrentRenderTarget = m_completeBubble;
            m_completeBubble.Clear(System.Drawing.Color.FromArgb(0, System.Drawing.Color.White));

            //Draw black triangle at the bottom
            VertexTypeList.PositionDiffuse2DTexture1[] blacktriangle = new VertexTypeList.PositionDiffuse2DTexture1[3];
            blacktriangle[0].Position.X = (m_completeBubble.Width / 2) - 10;
            blacktriangle[1].Position.X = (m_completeBubble.Width / 2) + 10;
            blacktriangle[2].Position.X = (m_completeBubble.Width / 2);
            blacktriangle[0].Position.Y = (m_completeBubble.Height - 15);
            blacktriangle[1].Position.Y = (m_completeBubble.Height - 15);
            blacktriangle[2].Position.Y = m_completeBubble.Height;
            blacktriangle[0].TextureCoordinates.X = 0.0f;
            blacktriangle[0].TextureCoordinates.Y = 0.0f;
            blacktriangle[0].Color = System.Drawing.Color.FromArgb(255, System.Drawing.Color.Black);
            blacktriangle[1].TextureCoordinates.X = 0.0f;
            blacktriangle[1].TextureCoordinates.Y = 0.0f;
            blacktriangle[1].Color = System.Drawing.Color.FromArgb(255, System.Drawing.Color.Black);
            blacktriangle[2].TextureCoordinates.X = 0.0f;
            blacktriangle[2].TextureCoordinates.Y = 0.0f;
            blacktriangle[2].Color = System.Drawing.Color.FromArgb(255, System.Drawing.Color.Black);
            m_completeBubble.Draw(blacktriangle);

            //Draw the side lines
            for (int i = 0; i < 1; i++)
            {
                m_completeBubble.Line(10, i, m_completeBubble.Width - 20, 1, System.Drawing.Color.Black);
                m_completeBubble.Line(m_completeBubble.Width - 1 - i, 10, 1, m_completeBubble.Height - 26, System.Drawing.Color.Black);
                m_completeBubble.Line(10, m_completeBubble.Height - 7 - i, m_completeBubble.Width - 20, 1, System.Drawing.Color.Black);
                m_completeBubble.Line(i, 10, 1, m_completeBubble.Height - 26, System.Drawing.Color.Black);
            }
            //Fill in the middle without polluting the corners.
            m_completeBubble.FilledRectangle(3, 1, m_completeBubble.Width - 6, m_completeBubble.Height - 8, System.Drawing.Color.White);
            m_completeBubble.FilledRectangle(1, 3, m_completeBubble.Width - 2, m_completeBubble.Height - 12, System.Drawing.Color.White);

            //Draw the white triangle at the bottom.
            VertexTypeList.PositionDiffuse2DTexture1[] whitetriangle = new VertexTypeList.PositionDiffuse2DTexture1[3];
            whitetriangle[0].Position.X = (m_completeBubble.Width / 2) - 7;
            whitetriangle[1].Position.X = (m_completeBubble.Width / 2) + 7;
            whitetriangle[2].Position.X = (m_completeBubble.Width / 2);
            whitetriangle[0].Position.Y = (m_completeBubble.Height - 15);
            whitetriangle[1].Position.Y = (m_completeBubble.Height - 15);
            whitetriangle[2].Position.Y = m_completeBubble.Height - 4;
            whitetriangle[0].TextureCoordinates.X = 0.0f;
            whitetriangle[0].TextureCoordinates.Y = 0.0f;
            whitetriangle[0].Color = System.Drawing.Color.FromArgb(255, System.Drawing.Color.White);
            whitetriangle[1].TextureCoordinates.X = 0.0f;
            whitetriangle[1].TextureCoordinates.Y = 0.0f;
            whitetriangle[1].Color = System.Drawing.Color.FromArgb(255, System.Drawing.Color.White);
            whitetriangle[2].TextureCoordinates.X = 0.0f;
            whitetriangle[2].TextureCoordinates.Y = 0.0f;
            whitetriangle[2].Color = System.Drawing.Color.FromArgb(255, System.Drawing.Color.White);
            m_completeBubble.Draw(whitetriangle);

            //Draw the corners.
            cornersprite.SourceBlend = AlphaBlendOperation.One;
            cornersprite.DestinationBlend = AlphaBlendOperation.Zero;
            cornersprite.VerticalFlip = true;
            cornersprite.SetPosition(0, 0);
            cornersprite.Draw();
            cornersprite.HorizontalFlip = true;
            cornersprite.SetPosition(m_completeBubble.Width - 16, 0);
            cornersprite.Draw();
            cornersprite.VerticalFlip = false;
            cornersprite.SetPosition(m_completeBubble.Width - 16, m_completeBubble.Height - 22);
            cornersprite.Draw();
            cornersprite.HorizontalFlip = false;
            cornersprite.SetPosition(0, m_completeBubble.Height - 22);
            cornersprite.Draw();
            textSprite.Draw();
            Gorgon.CurrentRenderTarget = target; // restore rendertarget
            spriteupdaterequired = false; //Sprite is now up to date
            millisecondsremaining = millisecondstolive;
            lastUpdate = DateTime.Now;
        }

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