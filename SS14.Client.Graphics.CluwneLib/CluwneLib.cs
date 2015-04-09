using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.Graphics.CluwneLib.Event;
using SS14.Client.Graphics.CluwneLib.Render;
using SS14.Client.Graphics.CluwneLib.Shader;
using SS14.Client.Graphics.CluwneLib.Timing;
using SS14.Shared.Maths;
using System;
using System.Drawing;
using Color = System.Drawing.Color;
using SColor = SFML.Graphics.Color;

namespace SS14.Client.Graphics.CluwneLib
{
    public class CluwneDebug {
        public int RenderingDelay=0;
        public bool TextBorders=false;
        public uint Fontsize=0;
    };
    public class CluwneLib
    {
        public static Viewport CurrentClippingViewport;
        private static Clock _timer;
        private static RenderTarget[] _currentTarget;
        private static System.Threading.Mutex SFML_Threadlock;
        public static event FrameEventHandler Idle;
        private Color DEFAULTCOLOR;
        public static CluwneDebug Debug;

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
            SFML_Threadlock = new System.Threading.Mutex();

            if (!IsInitialized)
            {
                Initialize();
            }
            Idle += (delegate(object sender, FrameEventArgs e) {
                SFML_Threadlock.ReleaseMutex();
                System.Threading.Thread.Sleep(10); // maybe pickup vsync here?
                SFML_Threadlock.WaitOne();
            });

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

            IsRunning = true;
        }

        public static void Initialize()
        {
            if (IsInitialized)
                Terminate();

            Debug = new CluwneDebug();

            SFML_Threadlock.WaitOne();

            IsInitialized = true;

            _currentTarget = new RenderTarget[5];

            _timer = new Clock();
            FrameStats = new TimingData(_timer);
        }

        public static void RequestGC(Action action) {
            SFML_Threadlock.WaitOne();
            action.Invoke();
            SFML_Threadlock.ReleaseMutex();
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

        public static void Terminate()
        {
            SFML_Threadlock.ReleaseMutex();
        }

        public static void RunIdle(object sender, FrameEventArgs e)
        {
            Idle(sender, e);
        }

        public static void Stop()
        {
            Console.WriteLine("CluwneLib: Stop() requested");
            IsRunning=false;
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

        /// <summary>
        /// Draws a Rectangle to the current RenderTarget
        /// </summary>
        /// <param name="posX">Pos X of rectangle </param>
        /// <param name="posY"> Pos Y of rectangle </param>
        /// <param name="WidthX"> Width X of rectangle </param>
        /// <param name="HeightY"> Height Y of rectangle </param>
        /// <param name="Color"> Fill Color </param>
        public static void drawRectangle(int posX, int posY, int WidthX, int HeightY, Color Color)
        {
            RectangleShape rectangle = new RectangleShape();
            rectangle.Position = new SFML.System.Vector2f(posX, posY);
            rectangle.Size = new SFML.System.Vector2f(WidthX, HeightY);
            rectangle.FillColor = SystemColorToSFML(Color);

            CurrentRenderTarget.Draw(rectangle);
            if (CluwneLib.Debug.RenderingDelay > 0)
            {
                CluwneLib.Screen.Display();
                System.Threading.Thread.Sleep(CluwneLib.Debug.RenderingDelay);
            }
        }

        /// <summary>
        /// Draws a Hollow Rectangle to the Current RenderTarget
        /// </summary>
        /// <param name="posX"> Pos X of rectangle </param>
        /// <param name="posY"> Pos Y of rectangle </param>
        /// <param name="widthX"> Width X of rectangle </param>
        /// <param name="heightY"> Height Y of rectangle </param>
        /// <param name="OutlineThickness"> Outline Thickness of rectangle </param>
        /// <param name="OutlineColor"> Outline Color </param>
        public static void drawHollowRectangle(int posX, int posY, int widthX, int heightY, float OutlineThickness, Color OutlineColor)
        {
            RectangleShape HollowRect = new RectangleShape();
            HollowRect.FillColor = SystemColorToSFML(Color.Transparent);
            HollowRect.Position = new Vector2f(posX, posY);
            HollowRect.Size = new Vector2f(widthX, heightY);
            HollowRect.OutlineThickness = OutlineThickness;
            HollowRect.OutlineColor = SystemColorToSFML(OutlineColor);

            CurrentRenderTarget.Draw(HollowRect);
            if (CluwneLib.Debug.RenderingDelay > 0)
            {
                CluwneLib.Screen.Display();
                System.Threading.Thread.Sleep(CluwneLib.Debug.RenderingDelay);
            }


        }
        #endregion

        #region Circle
        /// <summary>
        /// Draws a Filled Circle to the CurrentRenderTarget
        /// </summary>
        /// <param name="posX"> Pos X of Circle</param>
        /// <param name="posY"> Pos Y of Circle </param>
        /// <param name="radius"> Radius of Circle </param>
        /// <param name="color"> Fill Color </param>
        public static void drawCircle(int posX, int posY, int radius, Color color)
        {
            CircleShape Circle = new CircleShape();
            Circle.Position = new Vector2(posX, posY);
            Circle.Radius = radius;
            Circle.FillColor = SystemColorToSFML(color);

            CurrentRenderTarget.Draw(Circle);


        }
        /// <summary>
        /// Draws a Hollow Circle to the CurrentRenderTarget
        /// </summary>
        /// <param name="posX"> Pos X of Circle </param>
        /// <param name="posY"> Pos Y of Circle </param>
        /// <param name="radius"> Radius of Circle </param>
        /// <param name="OutlineThickness"> Thickness of Circle Outline </param>
        /// <param name="OutlineColor"> Circle outline Color </param>
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

        /// <summary>
        /// Draws a Filled Circle to the CurrentRenderTarget
        /// </summary>
        /// <param name="posX"> Pos X of Circle </param>
        /// <param name="posY"> Pos Y of Circle </param>
        /// <param name="radius"> Radius of Cirle </param>
        /// <param name="color"> Fill Color </param>
        /// <param name="vector2"></param>
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
        /// <summary>
        /// Draws a Filled Point to the CurrentRenderTarget
        /// </summary>
        /// <param name="posX"> Pos X of Point </param>
        /// <param name="posY"> Pos Y of Point </param>
        /// <param name="color"> Fill Color </param>
        public static void drawPoint(int posX, int posY, Color color)
        {
            RectangleShape Point = new RectangleShape();
            Point.Position = new Vector2(posX, posY);
            Point.Size = new Vector2(1, 1);
            Point.FillColor = SystemColorToSFML(color);


            CurrentRenderTarget.Draw(Point);
        }

        /// <summary>
        /// Draws a hollow Point to the CurrentRenderTarget
        /// </summary>
        /// <param name="posX"> Pos X of Point </param>
        /// <param name="posY"> Pos Y of Point </param>
        /// <param name="OutlineColor"> Outline Color </param>
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
        /// <summary>
        /// Draws a Line to the CurrentRenderTarget
        /// </summary>
        /// <param name="posX"> Pos X of Line </param>
        /// <param name="posY"> Pos Y of Line </param>
        /// <param name="rotate"> Line Rotation </param>
        /// <param name="thickness"> Line Thickness </param>
        /// <param name="Color"> Line Color </param>
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
        /// <summary>
        /// Converts a System.Drawing.Color to a SFML.Graphics.Color
        /// </summary>
        /// <param name="color"> System.Drawing.Color to convert </param>
        /// <returns></returns>
        public static SColor SystemColorToSFML(Color color)
        {
            SColor temp = new SColor(color.R, color.G, color.B, color.A);
            return temp;
        }

        /// <summary>
        /// Converts a SFML.Graphics.Color to a System.Drawing.Color
        /// </summary>
        /// <param name="color"> SFML.Graphics.Color to convert </param>
        /// <returns></returns>
        public static Color SFMLColorToSystem(SColor color)
        {
            Color temp = Color.FromArgb(color.R,color.G,color.B,color.A);
            return temp;
        }

        public static SColor ColorFromARGB(byte A, Color rgb) {
            return new SColor(A, rgb.R, rgb.G, rgb.B);
        }

        public static SColor ColorFromARGB(byte A, SColor rgb) {
            return new SColor(A, rgb.R, rgb.G, rgb.B);
        }
        /// <summary>
        /// Converts a Point to a Vector2
        /// </summary>
        /// <param name="point"> Point to convert </param>
        /// <returns></returns>
        public static Vector2 PointToVector2(Point point)
        {
            return new Vector2(point.X, point.Y);
        }
        #endregion
    }
}
