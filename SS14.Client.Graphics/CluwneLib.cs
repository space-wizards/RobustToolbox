using OpenTK.Graphics;
using SFML.Graphics;
using SFML.System;
using SS14.Client.Graphics.Event;
using SS14.Client.Graphics.Render;
using SS14.Client.Graphics.Settings;
using SS14.Client.Graphics.Shader;
using SS14.Client.Graphics.Timing;
using SS14.Client.Graphics.View;
using System;


namespace SS14.Client.Graphics
{
    public class CluwneLib
    {

        private static RenderTarget[] renderTargetArray;
        private static Clock _timer;
  
        public static event FrameEventHandler FrameEvent;
        public static Viewport CurrentClippingViewport;

        public delegate void EventHandler();
        public static event EventHandler RefreshVideoSettings;

        #region Accessors
        public static Vector2f WorldCenter { get; set; }
        public static Vector2u ScreenViewportSize { get; set; }
        public static int TileSize { get; set; }
        public static FloatRect WorldViewport
        {
            get
            {
                return ScreenToWorld(ScreenViewport);
            }
        }      
        public static IntRect ScreenViewport
        {
            get
            {
                return new IntRect(0, 0, (int)ScreenViewportSize.X, (int)ScreenViewportSize.Y);
            }
        }     

        public static bool IsInitialized { get; set; }
        public static bool IsRunning { get; set; }
        public static bool FrameStatsVisible { get; set; }
        
        public static CluwneWindow  Screen        { get; set; }
        public static TimingData    FrameStats    { get; set; }
        public static VideoSettings Video         { get; private set; }
        public static Debug Debug         { get; private set; }
        public static GLSLShader    CurrentShader { get; internal set; }

        public static BlendingModes BlendingMode { get; set; }    
        public static RenderTarget CurrentRenderTarget
        {
            get
            {
                if (renderTargetArray[0] == null)
                    renderTargetArray[0] = Screen;

                return renderTargetArray[0];
            }
            internal set
            {
                if (value == null)
                    value = Screen;

                setAdditionalRenderTarget(0, value);
            }
        }
      
        #endregion




        static CluwneLib()
        {
            Video = new VideoSettings();
            Debug = new Debug();        
        }

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

            FrameEvent += (delegate(object sender, FrameEventArgs e) {
               
                System.Threading.Thread.Sleep(10); // maybe pickup vsync here?
               
            });

            if ((Screen != null) && (renderTargetArray == null))
                throw new InvalidOperationException("Something has gone terribly wrong!");

            if (IsRunning)
                return;

            _timer.Restart();
            FrameStats.Reset();

            if (renderTargetArray != null)
            {
                for (int i = 0; i < renderTargetArray.Length; i++)
                {
                    if (renderTargetArray[0] == null)
                    {
                        renderTargetArray[0] = Screen;
                    }
                }

            }

            IsRunning = true;
        }

        public static void drawRectangle(int x, int y, int width, int height, object p)
        {
            throw new NotImplementedException();
        }

        public static void Initialize()
        {
            if (IsInitialized)
                Terminate();
           
            Screen = new CluwneWindow(CluwneLib.Video.getVideoMode(), "Developer Station 14", CluwneLib.Video.getWindowStyle());

            _timer = new Clock();
            FrameStats = new TimingData(_timer);
            renderTargetArray = new RenderTarget[5];
            CurrentClippingViewport = new Viewport(0, 0, Screen.Size.X, Screen.Size.Y);
            IsInitialized = true;



            //Hook OpenTK into SFMLs Opengl 
        OpenTK.Toolkit.Init(new OpenTK.ToolkitOptions{
                // Non-Native backend doesn't have a default GetAddress method
        Backend = OpenTK.PlatformBackend.PreferNative
        });
        new GraphicsContext(OpenTK.ContextHandle.Zero, null);
    }
       
        public static void RequestGC(Action action)
        {
          action.Invoke();         
        }           

  

        public static void ClearCurrentRendertarget(Color color)
        {
            CurrentRenderTarget.Clear(color);
        }

        public static void Terminate()
        {                   
            CurrentClippingViewport = null;
            IsInitialized = false;
            Screen.Close();
        }

        public static void RunIdle(object sender, FrameEventArgs e)
        {
            FrameEvent(sender, e);
        }

        public static void Stop()
        {
            Console.WriteLine("CluwneLib: Stop() requested");
            IsRunning=false;
        }


        public static void UpdateVideoSettings()
        {
           RefreshVideoSettings();
        }

        #endregion

        #region RenderTarget Stuff

     

        public static void setAdditionalRenderTarget(int index, RenderTarget _target)
        {
           renderTargetArray[index] = _target;
        }

        public static RenderTarget getAdditionalRenderTarget(int index)
        {
            return renderTargetArray[index];
        }


        /// <summary>
        /// resets the Current Render Target back to the screen
        /// </summary>
        public static void ResetRenderTarget()
        {
            CurrentRenderTarget = null; //sets it back to the screen
        }

        /// <summary>
        /// Clears the Shader
        /// </summary>
        public static void ResetShader()
        {
            CurrentShader = null;
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
            rectangle.FillColor = Color;

            CurrentRenderTarget.Draw(rectangle);           
        }

        /// <summary>
        /// Draws a Rectangle to the current RenderTarget
        /// </summary>
        /// <param name="posX">Pos X of rectangle </param>
        /// <param name="posY"> Pos Y of rectangle </param>
        /// <param name="WidthX"> Width X of rectangle </param>
        /// <param name="HeightY"> Height Y of rectangle </param>
        /// <param name="Color"> Fill Color </param>
        public static void drawRectangle(float posX, float posY, float WidthX, float HeightY, Color Color)
        {
            RectangleShape rectangle = new RectangleShape();
            rectangle.Position = new SFML.System.Vector2f(posX, posY);
            rectangle.Size = new SFML.System.Vector2f(WidthX, HeightY);
            rectangle.FillColor = Color;

            CurrentRenderTarget.Draw(rectangle);
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
            HollowRect.FillColor = Color.Transparent;
            HollowRect.Position = new SFML.System.Vector2f(posX, posY);
            HollowRect.Size = new SFML.System.Vector2f(widthX, heightY);
            HollowRect.OutlineThickness = OutlineThickness;
            HollowRect.OutlineColor = OutlineColor;

            CurrentRenderTarget.Draw(HollowRect);
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
            Circle.Position = new Vector2f(posX, posY);
            Circle.Radius = radius;
            Circle.FillColor = color;

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
        public static void drawHollowCircle(int posX, int posY, int radius,float OutlineThickness, Color OutlineColor)
        {
            CircleShape Circle = new CircleShape();
            Circle.Position = new Vector2f(posX, posY);
            Circle.Radius = radius;
            Circle.FillColor = Color.Transparent;
            Circle.OutlineThickness = OutlineThickness;
            Circle.OutlineColor = OutlineColor;

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
        public static void drawCircle(float posX, float posY, int radius, Color color, Vector2f vector2)
        {
            CircleShape Circle = new CircleShape();
            Circle.Position = new Vector2f(posX, posY);
            Circle.Radius = radius;
            Circle.FillColor = Color.Transparent;

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
            Point.Position = new Vector2f(posX, posY);
            Point.Size = new Vector2f(1, 1);
            Point.FillColor = color;

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
            hollowPoint.Position = new Vector2f(posX, posY);
            hollowPoint.Size = new Vector2f(1, 1);
            hollowPoint.FillColor = Color.Transparent;
            hollowPoint.OutlineThickness = .6f;
            hollowPoint.OutlineColor = OutlineColor;

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
            line.Position = new Vector2f(posX, posY);
            line.Rotation = rotate;
            line.OutlineThickness = thickness;
            line.FillColor = Color;

            CurrentRenderTarget.Draw(line);
        }

        #endregion

        #region Text
        /// <summary>
        /// Draws text to the Current RenderTarget
        /// </summary>
        /// <param name="posX"> Pos X of rectangle </param>
        /// <param name="posY"> Pos Y of rectangle </param>
        /// <param name="text"> Text to render </param>
        /// <param name="size"> Size of the font </param>
        /// <param name="textColour"> Colour of the text </param>
        public static void drawText(float posX, float posY, string text, uint size, Color textColour)
        {
            Text _text = new Text(text, new Font(@"..\..\Resources\Fonts\bluehigh.ttf"));
            _text.Position = new SFML.System.Vector2f(posX, posY);
            _text.Color = textColour;
            _text.CharacterSize = size;

            CurrentRenderTarget.Draw(_text);
        }
        #endregion

        #endregion

        #region Client Window Data  

        /// <summary>
        /// Transforms a point from the world (tile) space, to screen (pixel) space.
        /// </summary>
        public static Vector2f WorldToScreen(Vector2f point)
        {
            var center = WorldCenter;
            return new Vector2f(
               (point.X - center.X) * TileSize + ScreenViewportSize.X / 2,
               (point.Y - center.Y) * TileSize + ScreenViewportSize.Y / 2
               );
        }

        /// <summary>
        /// Transforms a rectangle from the world (tile) space, to screen (pixel) space.
        /// </summary>
        public static FloatRect WorldToScreen(FloatRect rect)
        {
            var center = WorldCenter;
            return new FloatRect(
                (rect.Left - center.X) * TileSize + ScreenViewportSize.X / 2,
                (rect.Top - center.Y) * TileSize + ScreenViewportSize.Y / 2,
                rect.Width * TileSize,
                rect.Height * TileSize
                );
        }

        public static Vector2f WorldToTile(Vector2f point)
        {
            return new Vector2f(
                (float)Math.Floor((decimal)point.X),
                (float)Math.Floor((decimal)point.Y)
                );
        }

        public static Vector2f TileToWorld(Vector2f point)
        {
            return new Vector2f(
                point.X + 0.5f,
                point.Y + 0.5f
                );
        }

        /// <summary>
        /// Transforms a point from the screen (pixel) space, to world (tile) space.
        /// </summary>
        public static Vector2f ScreenToWorld(Vector2i point)
        {
            var center = WorldCenter;
            return new Vector2f(
                ((ScreenViewportSize.X / 2 - (float)point.X) * -1) / TileSize + WorldCenter.X,
                ((ScreenViewportSize.Y / 2 - (float)point.Y) * -1) / TileSize + WorldCenter.Y
                );
        }
        /// <summary>
        /// Transforms a rectangle from the screen (pixel) space, to world (tile) space.
        /// </summary>
        public static FloatRect ScreenToWorld(IntRect rect)
       {
           var center = WorldCenter;
           return new FloatRect(
               ((ScreenViewportSize.X / 2 - (float)rect.Left) *-1) / TileSize + center.X,
               ((ScreenViewportSize.Y / 2 - (float)rect.Top) * -1) / TileSize + center.Y,
               rect.Width / TileSize,
               rect.Height / TileSize
               );
       }

        /// <summary>
        /// Scales a vector from pixel coordinates to tile coordinates.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Vector2f PixelToTile(Vector2f vec)
        {
            float x = vec.X / TileSize;
            float y = vec.Y / TileSize;
            return new Vector2f(x, y);
        }

        /// <summary>
        /// Scales a rectangle from pixel coordinates to tile coordinates.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static FloatRect PixelToTile(FloatRect rect)
       {
           return new FloatRect(
               rect.Left / TileSize,
               rect.Top / TileSize,
               rect.Width / TileSize,
               rect.Height / TileSize
               );
       }

        /// <summary>
        /// Takes a point in world (tile) coordinates, and rounds it to the nearest pixel.
        /// </summary>
        public static Vector2f GetNearestPixel(Vector2f worldPoint)
        {
            return new Vector2f(
                (float)Math.Round(worldPoint.X * TileSize) / TileSize,
                (float)Math.Round(worldPoint.Y * TileSize) / TileSize
                );
        }



        #endregion



    }
}
