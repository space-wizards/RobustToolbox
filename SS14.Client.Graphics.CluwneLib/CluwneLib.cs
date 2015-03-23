using System;
using System.Windows.Forms;
using SFML.Graphics;
using SFML.System;
using SS14.Client.Graphics.CluwneLib.Event;
using SS14.Client.Graphics.CluwneLib.Render;
using SS14.Client.Graphics.CluwneLib.Timing;
using Color = System.Drawing.Color;
using SColor = SFML.Graphics.Color;
using SS14.Client.Graphics.CluwneLib.Shader;
using SS14.Shared.Maths;
using System.Drawing;
using SFML.Window;
using System.Collections.Generic;
using System.Collections;

namespace SS14.Client.Graphics.CluwneLib
{
    public class CluwneLib
    {
        public static Viewport CurrentClippingViewport;
        private static Clock _timer;
        private static RenderTarget[] _currentTarget;
        public static event FrameEventHandler Idle;
        private Color DEFAULTCOLOR;

        #region Accessors
        public static bool IsInitialized { get; set; }
        public static bool IsRunning { get; set; }
        public static CluwneWindow Screen {  get;  set; }
        public static TimingData FrameStats { get; set; }
        public static FXShader CurrentShader { get; set; }
        public static BlendingModes BlendingMode { get; set; }
        public Styles Style { get; set; }
        #endregion
        
        #region CluwneEngine
        /// <summary>
        /// Start engine rendering.
        /// </summary>
        /// Shamelessly taken from Gorgon.
        public static void Go()
        {

            if (!IsInitialized)
            {
                Initialize();
            }

            if ((Screen != null) && (_currentTarget == null))
                throw new InvalidOperationException("The render target is invalid.");

            if (IsRunning)
                return;

            _timer.Restart();
            FrameStats.Reset();

            if (_currentTarget != null)
            {
                for (int i = 0; i < _currentTarget.Length; i++)
                {
                    if (_currentTarget[0] != null)     
                    {
                       //update targets and viewport
                    }
                }
                
            }
            
           
           
            Application.Idle += new EventHandler(Run);

            IsRunning = true;
        }

        public static void Initialize()
        {
            if (IsInitialized)
                Terminate();

            IsInitialized = true;

            _currentTarget = new RenderTarget[5];
        
            _timer = new Clock();
            FrameStats = new TimingData(_timer);


           
        }

        public static void SetMode(int displayWidth, int displayHeight)
        {
            Screen = new CluwneWindow(new VideoMode((uint)displayWidth, (uint)displayHeight), "Space station 14");
        }

        public static void SetMode(int width, int height, bool fullscreen, bool p4, bool p5, int refreshRate)
        {
            Styles stylesTemp = new Styles();

            if(fullscreen)
                stylesTemp = Styles.Fullscreen;
            else stylesTemp = Styles.Default;

            Screen = new CluwneWindow(new VideoMode((uint)width, (uint)height),"Space Station 14",stylesTemp);
        }


        public static void Clear(Color color)
        {
            CurrentRenderTarget.Clear(SystemColorToSFML(color));
        }

        private static void Terminate()
        {
            Screen.Close();
        }
        
        public static void Run(object sender, EventArgs e)
        {
               


        }

        public static void Stop()
        {
            CurrentRenderTarget = null;
            Screen.Dispose();
        }
       
        #endregion

        #region RenderTarget Stuff

        public static RenderTarget CurrentRenderTarget
        {
            get
            {
                if (_currentTarget[0] == null)
                    _currentTarget[0] = Screen;
                        
                return _currentTarget[0];
            }
            set
            {
                if (value == null)
                    value = Screen;

                setAdditionalRenderTarget(0, value);
            }
        }

        public static void setAdditionalRenderTarget(int index, RenderTarget _target)
        {
           _currentTarget[index] = _target;
        }

        public static RenderTarget getAdditionalRenderTarget(int index)
        {
            return _currentTarget[index];
        }


        #endregion


        #region Drawing Methods

        #region Rectangle
        public static void drawRectangle(int posX, int posY, int WidthX, int HeightY, Color Color)
        {
            RectangleShape rectangle = new RectangleShape();
            rectangle.Position = new SFML.System.Vector2f(posX, posY);
            rectangle.Size = new SFML.System.Vector2f(WidthX, HeightY);
            rectangle.FillColor = SystemColorToSFML(Color);

            CurrentRenderTarget.Draw(rectangle);


        }

     

        public static void drawHollowRectangle(int posX, int posY, int widthX, int heightY, float OutlineThickness, Color OutlineColor)
        {
            RectangleShape HollowRect = new RectangleShape();
            HollowRect.FillColor = SystemColorToSFML(Color.Transparent);
            HollowRect.Position = new Vector2f(posX, posY);
            HollowRect.Size = new Vector2f(widthX, heightY);
            HollowRect.OutlineThickness = OutlineThickness;
            HollowRect.OutlineColor = SystemColorToSFML(OutlineColor);

            CurrentRenderTarget.Draw(HollowRect);


        }
        #endregion

        #region Circle
        public static void drawCircle(int posX, int posY, int radius, Color color)
        {
            CircleShape Circle = new CircleShape();
            Circle.Position = new Vector2(posX, posY);
            Circle.Radius = radius;
            Circle.FillColor = SystemColorToSFML(color);

            CurrentRenderTarget.Draw(Circle);


        }

        public static void drawHollowCircle(int posX, int posY, int radius,float OutlineThickness ,Color OutlineColor)
        {
            CircleShape Circle = new CircleShape();
            Circle.Position = new Vector2(posX, posY);
            Circle.Radius = radius;
            Circle.FillColor = SystemColorToSFML(Color.Transparent);
            Circle.OutlineThickness = OutlineThickness;
            Circle.OutlineColor = SystemColorToSFML(OutlineColor);

            CurrentRenderTarget.Draw(Circle);
        }


        public static void drawCircle(float posX, float posY, int radius, Color color, Vector2 vector2)
        {
            CircleShape Circle = new CircleShape();
            Circle.Position = new Vector2(posX, posY);
            Circle.Radius = radius;
            Circle.FillColor = SystemColorToSFML(Color.Transparent);
        
            CurrentRenderTarget.Draw(Circle);
        }
        #endregion

        #region Point
        public static void drawPoint(int posX, int posY, Color color)
        {
            RectangleShape Point = new RectangleShape();
            Point.Position = new Vector2(posX, posY);
            Point.Size = new Vector2(1, 1);
            Point.FillColor = SystemColorToSFML(color);


            CurrentRenderTarget.Draw(Point);
        }

        public static void drawHollowPoint(int posX, int posY, Color OutlineColor)
        {
            RectangleShape hollowPoint = new RectangleShape();
            hollowPoint.Position = new Vector2(posX, posY);
            hollowPoint.Size = new Vector2(1, 1);
            hollowPoint.FillColor = SystemColorToSFML(Color.Transparent);
            hollowPoint.OutlineThickness = .6f;
            hollowPoint.OutlineColor = SystemColorToSFML(OutlineColor);

            CurrentRenderTarget.Draw(hollowPoint);
        }

        #endregion

        #region Line
        public static void drawLine(int posX, int posY, int rotate,float thickness, Color Color)
        {
            RectangleShape line = new RectangleShape();
            line.Position = new Vector2(posX,posY);
            line.Rotation = rotate;
            line.OutlineThickness = thickness;
            line.FillColor = SystemColorToSFML(Color);

            CurrentRenderTarget.Draw(line);
        }

        #endregion

        #endregion


        #region Helper Methods
        public static SColor SystemColorToSFML(Color color) // System Color  to SFML color 
        {
            SColor temp = new SColor(color.R, color.G, color.B, color.A);
            return temp;
        }

        public static Color SFMLColorToSystem(SColor color) // SFML color to System Color
        {
            Color temp = Color.FromArgb(color.R,color.G,color.B,color.A);
                      
            return temp;
        }

        public static Vector2 PointToVector2(Point point)
        {


            return new Vector2(point.X, point.Y);
        }

        #endregion


    }
}
