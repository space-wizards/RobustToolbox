using OpenTK;
using OpenTK.Graphics;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.Graphics.Render;
using SS14.Client.Graphics.Settings;
using SS14.Client.Graphics.Shader;
using SS14.Client.Graphics.Utility;
using SS14.Client.Graphics.View;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Timing;
using System;
using Vector2i = SS14.Shared.Maths.Vector2i;
using Vector2u = SS14.Shared.Maths.Vector2u;

namespace SS14.Client.Graphics
{
    public class CluwneLib
    {
        private static RenderTarget[] renderTargetArray;

        public static GameTiming Time { get; private set; }
        public static event EventHandler<FrameEventArgs> FrameEvent;
        public static Viewport CurrentClippingViewport;

        public delegate void EventHandler();
        public static event EventHandler RefreshVideoSettings;

        #region Accessors
        public static Vector2 WorldCenter { get; set; }
        public static Vector2u ScreenViewportSize { get; set; }

        /// <summary>
        /// Viewport scaling
        /// </summary>
        public static int TileSize { get; set; } = 32;

        public static Box2 WorldViewport
        {
            get
            {
                return ScreenToWorld(ScreenViewport);
            }
        }
        public static Box2i ScreenViewport
        {
            get
            {
                return Box2i.FromDimensions(0, 0, (int)ScreenViewportSize.X, (int)ScreenViewportSize.Y);
            }
        }

        public static bool IsInitialized { get; set; }
        public static bool IsRunning { get; set; }
        public static bool FrameStatsVisible { get; set; }

        public static CluwneWindow SplashScreen { get; set; }
        public static CluwneWindow Screen { get; set; }
        public static VideoSettings Video { get; private set; }
        public static Debug Debug { get; private set; }
        public static GLSLShader CurrentShader { get; internal set; }

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

        #endregion Accessors

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

            if ((Screen != null) && (renderTargetArray == null))
                throw new InvalidOperationException("Something has gone terribly wrong!");

            if (IsRunning)
                return;

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

        public static CluwneWindow ShowSplashScreen(VideoMode vMode)
        {
            if (SplashScreen == null)
            {
                SplashScreen = new CluwneWindow(vMode, "Space Station 14", Styles.None);
            }

            return SplashScreen;
        }

        public static void CleanupSplashScreen()
        {
            SplashScreen.Close();
            SplashScreen = null;
        }

        public static void Initialize()
        {
            if (IsInitialized)
                Terminate();

            Time = new GameTiming();

            Screen = new CluwneWindow(CluwneLib.Video.getVideoMode(), "Developer Station 14", CluwneLib.Video.getWindowStyle());
            Screen.SetVerticalSyncEnabled(true);
            Screen.SetFramerateLimit(300);

            renderTargetArray = new RenderTarget[5];
            CurrentClippingViewport = new Viewport(0, 0, Screen.Size.X, Screen.Size.Y);
            IsInitialized = true;

            //Hook OpenTK into SFMLs Opengl
            OpenTK.Toolkit.Init(new OpenTK.ToolkitOptions
            {
                // Non-Native backend doesn't have a default GetAddress method
                Backend = OpenTK.PlatformBackend.PreferNative
            });
            new GraphicsContext(OpenTK.ContextHandle.Zero, null);
        }

        public static void RequestGC(Action action)
        {
            action.Invoke();
        }

        public static void ClearCurrentRendertarget(Color4 color)
        {
            CurrentRenderTarget.Clear(color.Convert());
        }

        public static void Terminate()
        {
            CurrentClippingViewport = null;
            IsInitialized = false;
            Screen.Close();
        }

        public static void RunIdle(object sender, FrameEventArgs e)
        {
            FrameEvent?.Invoke(sender, e);
        }

        public static void Stop()
        {
            Console.WriteLine("CluwneLib: Stop() requested");
            IsRunning = false;
        }

        public static void UpdateVideoSettings()
        {
            RefreshVideoSettings();
        }

        #endregion CluwneEngine

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

        #endregion RenderTarget Stuff

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
        public static void drawRectangle(int posX, int posY, int WidthX, int HeightY, Color4 Color)
        {
            RectangleShape rectangle = new RectangleShape();
            rectangle.Position = new SFML.System.Vector2f(posX, posY);
            rectangle.Size = new SFML.System.Vector2f(WidthX, HeightY);
            rectangle.FillColor = Color.Convert();

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
        public static void drawRectangle(float posX, float posY, float WidthX, float HeightY, Color4 Color)
        {
            RectangleShape rectangle = new RectangleShape();
            rectangle.Position = new SFML.System.Vector2f(posX, posY);
            rectangle.Size = new SFML.System.Vector2f(WidthX, HeightY);
            rectangle.FillColor = Color.Convert();

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
        public static void drawHollowRectangle(int posX, int posY, int widthX, int heightY, float OutlineThickness, Color4 OutlineColor)
        {
            RectangleShape HollowRect = new RectangleShape();
            HollowRect.FillColor = Color.Transparent;
            HollowRect.Position = new SFML.System.Vector2f(posX, posY);
            HollowRect.Size = new SFML.System.Vector2f(widthX, heightY);
            HollowRect.OutlineThickness = OutlineThickness;
            HollowRect.OutlineColor = OutlineColor.Convert();

            CurrentRenderTarget.Draw(HollowRect);
        }
        #endregion Rectangle

        #region Circle
        /// <summary>
        /// Draws a Filled Circle to the CurrentRenderTarget
        /// </summary>
        /// <param name="posX"> Pos X of Circle</param>
        /// <param name="posY"> Pos Y of Circle </param>
        /// <param name="radius"> Radius of Circle </param>
        /// <param name="color"> Fill Color </param>
        public static void drawCircle(int posX, int posY, int radius, Color4 color)
        {
            CircleShape Circle = new CircleShape();
            Circle.Position = new Vector2f(posX, posY);
            Circle.Radius = radius;
            Circle.FillColor = color.Convert();

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
        public static void drawHollowCircle(int posX, int posY, int radius, float OutlineThickness, Color4 OutlineColor)
        {
            CircleShape Circle = new CircleShape();
            Circle.Position = new Vector2f(posX - radius, posY - radius);
            Circle.Radius = radius;
            Circle.FillColor = Color.Transparent;
            Circle.OutlineThickness = OutlineThickness;
            Circle.OutlineColor = OutlineColor.Convert();

            CurrentRenderTarget.Draw(Circle);
        }

        #endregion Circle

        #region Point
        /// <summary>
        /// Draws a Filled Point to the CurrentRenderTarget
        /// </summary>
        /// <param name="posX"> Pos X of Point </param>
        /// <param name="posY"> Pos Y of Point </param>
        /// <param name="color"> Fill Color </param>
        public static void drawPoint(int posX, int posY, Color4 color)
        {
            RectangleShape Point = new RectangleShape();
            Point.Position = new Vector2f(posX, posY);
            Point.Size = new Vector2f(1, 1);
            Point.FillColor = color.Convert();

            CurrentRenderTarget.Draw(Point);
        }

        /// <summary>
        /// Draws a hollow Point to the CurrentRenderTarget
        /// </summary>
        /// <param name="posX"> Pos X of Point </param>
        /// <param name="posY"> Pos Y of Point </param>
        /// <param name="OutlineColor"> Outline Color </param>
        public static void drawHollowPoint(int posX, int posY, Color4 OutlineColor)
        {
            RectangleShape hollowPoint = new RectangleShape();
            hollowPoint.Position = new Vector2f(posX, posY);
            hollowPoint.Size = new Vector2f(1, 1);
            hollowPoint.FillColor = Color.Transparent;
            hollowPoint.OutlineThickness = .6f;
            hollowPoint.OutlineColor = OutlineColor.Convert();

            CurrentRenderTarget.Draw(hollowPoint);
        }

        #endregion Point

        #region Line
        /// <summary>
        /// Draws a Line to the CurrentRenderTarget
        /// </summary>
        /// <param name="posX"> Pos X of Line </param>
        /// <param name="posY"> Pos Y of Line </param>
        /// <param name="rotate"> Line Rotation </param>
        /// <param name="thickness"> Line Thickness </param>
        /// <param name="Color"> Line Color </param>
        public static void drawLine(float posX, float posY, float length, float rotate, float thickness, Color4 Color)
        {
            RectangleShape line = new RectangleShape();
            line.Position = new Vector2f(posX, posY);
            line.Size = new Vector2f(length, thickness);
            line.Rotation = rotate;
            line.OutlineThickness = thickness;
            line.FillColor = Color.Convert();
            line.OutlineColor = Color.Convert();

            CurrentRenderTarget.Draw(line);
        }

        #endregion Line

        #region Text
        /// <summary>
        /// Draws text to the Current RenderTarget
        /// </summary>
        /// <param name="posX"> Pos X of rectangle </param>
        /// <param name="posY"> Pos Y of rectangle </param>
        /// <param name="text"> Text to render </param>
        /// <param name="size"> Size of the font </param>
        /// <param name="textColor"> Color of the text </param>
        public static void drawText(float posX, float posY, string text, uint size, Color4 textColor, Font font)
        {
            Text _text = new Text(text, font);
            _text.Position = new SFML.System.Vector2f(posX, posY);
            _text.FillColor = textColor.Convert();
            _text.CharacterSize = size;

            CurrentRenderTarget.Draw(_text);
        }
        #endregion Text

        #endregion Drawing Methods

        #region Client Window Data

        /// <summary>
        /// Transforms a point from the world (tile) space, to screen (pixel) space.
        /// </summary>
        public static Vector2 WorldToScreen(WorldCoordinates point) //TODO: move to another coordinate type
        {
            var center = WorldCenter;
            return (point.Position - center) * TileSize + ScreenViewportSize / 2;
        }

        /// <summary>
        /// Transforms a rectangle from the world (tile) space, to screen (pixel) space.
        /// </summary>
        public static Box2 WorldToScreen(Box2 rect)
        {
            var center = WorldCenter;
            var topLeft = new Vector2(rect.Left, rect.Top);
            var bottomRight = new Vector2(rect.Right, rect.Bottom);
            return new Box2(
                WorldToScreen(topLeft),
                WorldToScreen(bottomRight)
            );
        }

        public static Vector2 WorldToTile(Vector2 point)
        {
            return new Vector2(
                (float)Math.Floor(point.X),
                (float)Math.Floor(point.Y)
            );
        }

        public static Vector2 TileToWorld(Vector2 point)
        {
            return new Vector2(
                point.X + 0.5f,
                point.Y + 0.5f
            );
        }

        /// <summary>
        /// Transforms a point from the screen (pixel) space, to world (tile) space.
        /// </summary>
        public static WorldCoordinates ScreenToWorld(ScreenCoordinates point)
        {
            return new WorldCoordinates((point.Position - ScreenViewportSize / 2) / TileSize + WorldCenter, point.MapID);
        }

        /// <summary>
        /// Transforms a rectangle from the screen (pixel) space, to world (tile) space.
        /// </summary>
        public static Box2 ScreenToWorld(Box2i rect)
        {
            var center = WorldCenter;
            return new Box2(
                ((Vector2)rect.TopLeft - ScreenViewportSize / 2) / TileSize + center,
                ((Vector2)rect.BottomRight - ScreenViewportSize / 2) / TileSize + center
            );
        }

        /// <summary>
        /// Scales a vector from pixel coordinates to tile coordinates.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Vector2 PixelToTile(Vector2 vec)
        {
            return vec / TileSize;
        }

        /// <summary>
        /// Scales a rectangle from pixel coordinates to tile coordinates.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Box2 PixelToTile(Box2 rect)
        {
            return new Box2(
                rect.Left / TileSize,
                rect.Top / TileSize,
                rect.Right / TileSize,
                rect.Bottom / TileSize
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

        #endregion Client Window Data
    }
}
