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

namespace SS14.Client.Graphics.CluwneLib
{
    public class CluwneLib
    {
        public static Viewport CurrentClippingViewport;
        private static Clock _timer;
        private static RenderTarget[] _currentTarget;
        public static event FrameEventHandler Idle;
        private Color DEFAULTCOLOR;

        /// <summary>
        /// Start engine rendering.
        /// </summary>
        /// Shamelessly taken from Gorgon.
        public static void Go()
        {
           

            if (!IsInitialized)
                ; //TODO: Throw exception

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
                    if (_currentTarget[i] != null)
                    {
                        
                    }
                }
                
            }
            
           
           
            Application.Idle += new EventHandler(Run);

            IsRunning = true;
        }

        public static CluwneWindow Screen { get; set; }

        public static bool IsRunning { get; set; }

        public static void Initialize()
        {
            if (IsInitialized)
                Terminate();

            IsInitialized = true;

            _timer = new Clock();

            FrameStats = new TimingData(_timer);
        }

        private static void Terminate()
        {
            throw new NotImplementedException();
        }

        public static TimingData FrameStats { get; set; }

        public static bool IsInitialized { get; set; }


       


     
        


        public static void Run(object sender, EventArgs e)
        {
            
        }

        public static void SetMode(Form mainWindow, int displayWidth, int displayHeight, bool b, bool b1, bool b2, int refresh)
        {
         =
        }

        public float Width
        {
            get;
            set;
        }
        public static RenderTarget CurrentRenderTarget
        {
            get { return _currentTarget[0]; }
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




        public static void Stop()
        {
            throw new NotImplementedException();
        }

        public static FXShader CurrentShader { get; set; }

        public static void drawRectangle(int posX, int posY, int WidthX, int HeightY, Color Color)
        {
            RectangleShape rectangle = new RectangleShape();
            rectangle.Position = new SFML.System.Vector2f(posX, posY);
            rectangle.Size = new SFML.System.Vector2f(WidthX, HeightY);
            rectangle.FillColor = SystemColorToSFML(Color);

            CluwneLib.CurrentRenderTarget.Draw(rectangle);


        }

     

        public void DrawHollowRectangle(int x, int y, int width, int height)
        {
            RectangleShape Rect = new RectangleShape();
            Rect.FillColor = new SFML.Graphics.Color(128,128,128);
            Rect.Position = new Vector2f(x, y);
            Rect.Size = new Vector2f(width, height);

            CluwneLib.CurrentRenderTarget.Draw(Rect);


        }


      

        public static BlendingModes BlendingMode { get; set; }

        public static void drawCircle(int p1, int p2, int p3, Color color)
        {
            throw new NotImplementedException();
        }

        public static void drawPoint(int p1, int p2, Color color)
        {
            throw new NotImplementedException();
        }

        public static void Clear(Color color)
        {
            throw new NotImplementedException();
        }

        public static void drawCircle(float p1, float p2, int p3, Color color, Shared.Maths.Vector2 vector2)
        {
           
        }

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
      
    }
}
