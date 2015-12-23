using System;
using SFML.Graphics;
using SFML.System;
using SS14.Client.Graphics.Event;
using SS14.Client.Graphics.Render;
using SS14.Client.Graphics.Timing;
using SystemColor = System.Drawing.Color;
using SFMLColor = SFML.Graphics.Color;
using SS14.Client.Graphics.Shader;
using SS14.Shared.Maths;
using System.Drawing;
using SFML.Window;
using System.Collections.Generic;
using System.Collections;
using SS14.Client.Graphics.Settings;
using SS14.Client.Graphics.View;

using OpenTK.Graphics;

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
        public static Vector2 WorldCenter { get; set; }
        public static SizeF ScreenViewportSize { get; set; }
        public static int TileSize { get; set; }
        public static RectangleF WorldViewport
        {
            get
            {
                return ScreenToWorld(ScreenViewport);
            }
        }      
        public static RectangleF ScreenViewport
        {
            get
            {
                return new RectangleF(PointF.Empty, ScreenViewportSize);
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
            var wi = OpenTK.Platform.Utilities.CreateWindowsWindowInfo(Screen.SystemHandle);
            var ctx = new OpenTK.Graphics.GraphicsContext(OpenTK.Graphics.GraphicsMode.Default, wi);
            ctx.MakeCurrent(wi);
            ctx.LoadAll();
        }
       
        public static void RequestGC(Action action)
        {
          action.Invoke();         
        }           

  

        public static void ClearCurrentRendertarget(SystemColor color)
        {
            CurrentRenderTarget.Clear(color.ToSFMLColor());
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
        public static void drawRectangle(int posX, int posY, int WidthX, int HeightY, SystemColor Color)
        {
            RectangleShape rectangle = new RectangleShape();
            rectangle.Position = new SFML.System.Vector2f(posX, posY);
            rectangle.Size = new SFML.System.Vector2f(WidthX, HeightY);
            rectangle.FillColor = Color.ToSFMLColor();

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
        public static void drawRectangle(float posX, float posY, float WidthX, float HeightY, SystemColor Color)
        {
            RectangleShape rectangle = new RectangleShape();
            rectangle.Position = new SFML.System.Vector2f(posX, posY);
            rectangle.Size = new SFML.System.Vector2f(WidthX, HeightY);
            rectangle.FillColor = Color.ToSFMLColor();

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
        public static void drawHollowRectangle(int posX, int posY, int widthX, int heightY, float OutlineThickness, SystemColor OutlineColor)
        {
            RectangleShape HollowRect = new RectangleShape();
            HollowRect.FillColor = SystemColor.Transparent.ToSFMLColor();
            HollowRect.Position = new Vector2f(posX, posY);
            HollowRect.Size = new Vector2f(widthX, heightY);
            HollowRect.OutlineThickness = OutlineThickness;
            HollowRect.OutlineColor = OutlineColor.ToSFMLColor();

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
        public static void drawCircle(int posX, int posY, int radius, SystemColor color)
        {
            CircleShape Circle = new CircleShape();
            Circle.Position = new Vector2(posX, posY);
            Circle.Radius = radius;
            Circle.FillColor = color.ToSFMLColor();

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
        public static void drawHollowCircle(int posX, int posY, int radius,float OutlineThickness ,SystemColor OutlineColor)
        {
            CircleShape Circle = new CircleShape();
            Circle.Position = new Vector2(posX, posY);
            Circle.Radius = radius;
            Circle.FillColor = SystemColor.Transparent.ToSFMLColor();
            Circle.OutlineThickness = OutlineThickness;
            Circle.OutlineColor = OutlineColor.ToSFMLColor();

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
        public static void drawCircle(float posX, float posY, int radius, SystemColor color, Vector2 vector2)
        {
            CircleShape Circle = new CircleShape();
            Circle.Position = new Vector2(posX, posY);
            Circle.Radius = radius;
            Circle.FillColor = SystemColor.Transparent.ToSFMLColor();

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
        public static void drawPoint(int posX, int posY, SystemColor color)
        {
            RectangleShape Point = new RectangleShape();
            Point.Position = new Vector2(posX, posY);
            Point.Size = new Vector2(1, 1);
            Point.FillColor = color.ToSFMLColor();

            CurrentRenderTarget.Draw(Point);
        }

        /// <summary>
        /// Draws a hollow Point to the CurrentRenderTarget
        /// </summary>
        /// <param name="posX"> Pos X of Point </param>
        /// <param name="posY"> Pos Y of Point </param>
        /// <param name="OutlineColor"> Outline Color </param>
        public static void drawHollowPoint(int posX, int posY, SystemColor OutlineColor)
        {
            RectangleShape hollowPoint = new RectangleShape();
            hollowPoint.Position = new Vector2(posX, posY);
            hollowPoint.Size = new Vector2(1, 1);
            hollowPoint.FillColor = SystemColor.Transparent.ToSFMLColor();
            hollowPoint.OutlineThickness = .6f;
            hollowPoint.OutlineColor = OutlineColor.ToSFMLColor();

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
        public static void drawLine(int posX, int posY, int rotate,float thickness, SystemColor Color)
        {
            RectangleShape line = new RectangleShape();
            line.Position = new Vector2(posX,posY);
            line.Rotation = rotate;
            line.OutlineThickness = thickness;
            line.FillColor = Color.ToSFMLColor();

            CurrentRenderTarget.Draw(line);
        }

        #endregion
   
        #endregion

        #region Client Window Data  
   
       /// <summary>
       /// Transforms a point from the world (tile) space, to screen (pixel) space.
       /// </summary>
       public static PointF WorldToScreen(PointF point)
       {
           var center = WorldCenter;
           return new PointF(
               (point.X - center.X) * TileSize + ScreenViewportSize.Width / 2,
               (point.Y - center.Y) * TileSize + ScreenViewportSize.Height / 2
               );
       }
       /// <summary>
       /// Transforms a point from the world (tile) space, to screen (pixel) space.
       /// </summary>
       public static Vector2 WorldToScreen(Vector2 point)
       {
           var center = WorldCenter;
           return new Vector2(
               (point.X - center.X) * TileSize + ScreenViewportSize.Width / 2,
               (point.Y - center.Y) * TileSize + ScreenViewportSize.Height / 2
               );
       }
       /// <summary>
       /// Transforms a rectangle from the world (tile) space, to screen (pixel) space.
       /// </summary>
       public static RectangleF WorldToScreen(RectangleF rect)
       {
           var center = WorldCenter;
           return new RectangleF(
               (rect.X - center.X) * TileSize + ScreenViewportSize.Width / 2,
               (rect.Y - center.Y) * TileSize + ScreenViewportSize.Height / 2,
               rect.Width * TileSize,
               rect.Height * TileSize
               );
       }

       /// <summary>
       /// Transforms a point from the screen (pixel) space, to world (tile) space.
       /// </summary>
       public static PointF ScreenToWorld(PointF point)
       {
           var center = WorldCenter;
           return new PointF(
               (point.X - ScreenViewportSize.Width / 2) / TileSize + center.X,
               (point.Y - ScreenViewportSize.Height / 2) / TileSize + center.Y
               );
       }
       /// <summary>
       /// Transforms a point from the screen (pixel) space, to world (tile) space.
       /// </summary>
       public static Vector2 ScreenToWorld(Vector2 point)
       {
           var center = WorldCenter;
           return new Vector2(
               (point.X - ScreenViewportSize.Width / 2) / TileSize + center.X,
               (point.Y - ScreenViewportSize.Height / 2) / TileSize + center.Y
               );
       }
       /// <summary>
       /// Transforms a rectangle from the screen (pixel) space, to world (tile) space.
       /// </summary>
       public static RectangleF ScreenToWorld(RectangleF rect)
       {
           var center = WorldCenter;
           return new RectangleF(
               (rect.X - ScreenViewportSize.Width / 2) / TileSize + center.X,
               (rect.Y - ScreenViewportSize.Height / 2) / TileSize + center.Y,
               rect.Width / TileSize,
               rect.Height / TileSize
               );
       }

       /// <summary>
       /// Scales a size from pixel coordinates to tile coordinates.
       /// </summary>
       /// <param name="size"></param>
       /// <returns></returns>
       public static SizeF PixelToTile(SizeF size)
       {
           return new SizeF(
               size.Width / TileSize,
               size.Height / TileSize
               );
       }
       /// <summary>
       /// Scales a vector from pixel coordinates to tile coordinates.
       /// </summary>
       /// <param name="size"></param>
       /// <returns></returns>
       public static Vector2 PixelToTile(Vector2 vec)
       {
           return new Vector2(
               vec.X / TileSize,
               vec.Y / TileSize
               );
       }
       /// <summary>
       /// Scales a rectangle from pixel coordinates to tile coordinates.
       /// </summary>
       /// <param name="size"></param>
       /// <returns></returns>
       public static RectangleF PixelToTile(RectangleF rect)
       {
           return new RectangleF(
               rect.X / TileSize,
               rect.Y / TileSize,
               rect.Width / TileSize,
               rect.Height / TileSize
               );
       }

       /// <summary>
       /// Takes a point in world (tile) coordinates, and rounds it to the nearest pixel.
       /// </summary>
       public static PointF GetNearestPixel(PointF worldPoint)
       {
           return new PointF(
               (float)Math.Round(worldPoint.X * TileSize) / TileSize,
               (float)Math.Round(worldPoint.Y * TileSize) / TileSize
               );
       }
       /// <summary>
       /// Takes a point in world (tile) coordinates, and rounds it to the nearest pixel.
       /// </summary>
       public static Vector2 GetNearestPixel(Vector2 worldPoint)
       {
           return new Vector2(
               (float)Math.Round(worldPoint.X * TileSize) / TileSize,
               (float)Math.Round(worldPoint.Y * TileSize) / TileSize
               );
       }
       
       #endregion


       
    }


    internal static class Conversions
    {
        public static SFMLColor ToSFMLColor(this SystemColor SystemColor)
        {
            return new SFMLColor(SystemColor.R,SystemColor.G,SystemColor.B,SystemColor.A);
        }

        public static SystemColor ToSystemColor(this SFMLColor SFMLColor)
        {
            SystemColor temp = SystemColor.FromArgb(SFMLColor.A, SFMLColor.R, SFMLColor.G, SFMLColor.B);
            return temp;
        }
      
        public static Vector2 ToVector2(this Point point)
        {
            return new Vector2(point.X, point.Y);
        }
    }
}
