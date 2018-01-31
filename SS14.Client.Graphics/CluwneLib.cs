using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.Graphics.Input;
using SS14.Client.Graphics.Render;
using SS14.Client.Graphics.Settings;
using SS14.Client.Graphics.Shader;
using SS14.Client.Graphics.Utility;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Timing;
using System;
using Color = SS14.Shared.Maths.Color;
using Vector2i = SS14.Shared.Maths.Vector2i;
using VideoMode = SS14.Client.Graphics.Render.VideoMode;
using Font = SS14.Client.Graphics.Sprites.Font;
using SS14.Client.Graphics.Views;

namespace SS14.Client.Graphics
{
    public class CluwneLib
    {
        private static IRenderTarget[] renderTargetArray;

        public static GameTiming Time { get; private set; }
        public static event EventHandler<FrameEventArgs> FrameEvent;
        public delegate void EventHandler();
        public static event EventHandler RefreshVideoSettings;

        public static Box2 WorldViewport => ScreenToWorld(Box2i.FromDimensions(0, 0, (int)Window.Viewport.Size.X, (int)Window.Viewport.Size.Y));

        private static bool IsInitialized { get; set; }
        public static bool IsRunning { get; private set; }
        public static bool FrameStatsVisible { get; set; }

        public static InputEvents Input { get; internal set; }
        private static CluwneWindow SplashScreen { get; set; }
        public static CluwneWindow Window { get; private set; }
        public static VideoSettings Video { get; }
        public static Debug Debug { get; }
        public static GLSLShader CurrentShader { get; internal set; }
        public static Camera Camera { get; } = new Camera();

        public static BlendingModes BlendingMode { get; set; }
        public static Render.RenderStates ShaderRenderState => new Render.RenderStates(CurrentShader);
        public static IRenderTarget CurrentRenderTarget
        {
            get
            {
                if (renderTargetArray[0] == null)
                    renderTargetArray[0] = Window;

                return renderTargetArray[0];
            }
            internal set
            {
                setAdditionalRenderTarget(0, value ?? Window);
            }
        }

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

            if ((Window != null) && (renderTargetArray == null))
                throw new InvalidOperationException("Something has gone terribly wrong!");

            if (IsRunning)
                return;

            if (renderTargetArray != null)
            {
                for (int i = 0; i < renderTargetArray.Length; i++)
                {
                    if (renderTargetArray[0] == null)
                    {
                        renderTargetArray[0] = Window;
                    }
                }
            }

            IsRunning = true;
        }

        public static CluwneWindow ShowSplashScreen(VideoMode vMode)
        {
            if (SplashScreen != null)
                return SplashScreen;

            var video = new VideoSettings(vMode);
            return SplashScreen = new CluwneWindow(new RenderWindow(vMode.SFMLVideoMode, "Space Station 14", Styles.None), video);
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

            var video = Video;
            var wind = new RenderWindow(video.GetVideoMode().SFMLVideoMode, "Developer Station 14", video.GetWindowStyle());
            Window = new CluwneWindow(wind, video);
            Window.Graphics.SetVerticalSyncEnabled(true);

            renderTargetArray = new IRenderTarget[5];
            IsInitialized = true;
        }

        public static void ClearCurrentRendertarget(Color color)
        {
            CurrentRenderTarget.Clear(color);
        }

        public static void Terminate()
        {
            IsInitialized = false;
            Window.Close();
        }

        public static void RunIdle(object sender, FrameEventArgs e)
        {
            FrameEvent?.Invoke(null, e);
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

        public static void setAdditionalRenderTarget(int index, IRenderTarget _target)
        {
            renderTargetArray[index] = _target;
        }

        public static IRenderTarget getAdditionalRenderTarget(int index)
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
        public static void drawRectangle(int posX, int posY, int WidthX, int HeightY, Color Color)
        {
            RectangleShape rectangle = new RectangleShape();
            rectangle.Position = new SFML.System.Vector2f(posX, posY);
            rectangle.Size = new SFML.System.Vector2f(WidthX, HeightY);
            rectangle.FillColor = Color.Convert();

            CurrentRenderTarget.DrawSFML(rectangle);
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
            rectangle.FillColor = Color.Convert();

            CurrentRenderTarget.DrawSFML(rectangle);
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
            HollowRect.FillColor = Color.Transparent.Convert();
            HollowRect.Position = new SFML.System.Vector2f(posX, posY);
            HollowRect.Size = new SFML.System.Vector2f(widthX, heightY);
            HollowRect.OutlineThickness = OutlineThickness;
            HollowRect.OutlineColor = OutlineColor.Convert();

            CurrentRenderTarget.DrawSFML(HollowRect);
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
        public static void drawCircle(int posX, int posY, int radius, Color color)
        {
            CircleShape Circle = new CircleShape();
            Circle.Position = new Vector2f(posX, posY);
            Circle.Radius = radius;
            Circle.FillColor = color.Convert();

            CurrentRenderTarget.DrawSFML(Circle);
        }
        /// <summary>
        /// Draws a Hollow Circle to the CurrentRenderTarget
        /// </summary>
        /// <param name="posX"> Pos X of Circle </param>
        /// <param name="posY"> Pos Y of Circle </param>
        /// <param name="radius"> Radius of Circle </param>
        /// <param name="OutlineThickness"> Thickness of Circle Outline </param>
        /// <param name="OutlineColor"> Circle outline Color </param>
        public static void drawHollowCircle(int posX, int posY, int radius, float OutlineThickness, Color OutlineColor)
        {
            CircleShape Circle = new CircleShape();
            Circle.Position = new Vector2f(posX - radius, posY - radius);
            Circle.Radius = radius;
            Circle.FillColor = Color.Transparent.Convert();
            Circle.OutlineThickness = OutlineThickness;
            Circle.OutlineColor = OutlineColor.Convert();

            CurrentRenderTarget.DrawSFML(Circle);
        }

        #endregion Circle

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
            Point.FillColor = color.Convert();

            CurrentRenderTarget.DrawSFML(Point);
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
            hollowPoint.FillColor = Color.Transparent.Convert();
            hollowPoint.OutlineThickness = .6f;
            hollowPoint.OutlineColor = OutlineColor.Convert();

            CurrentRenderTarget.DrawSFML(hollowPoint);
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
        public static void drawLine(float posX, float posY, float length, float rotate, float thickness, Color Color)
        {
            RectangleShape line = new RectangleShape();
            line.Position = new Vector2f(posX, posY);
            line.Size = new Vector2f(length, thickness);
            line.Rotation = rotate;
            line.OutlineThickness = thickness;
            line.FillColor = Color.Convert();
            line.OutlineColor = Color.Convert();

            CurrentRenderTarget.DrawSFML(line);
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
        public static void drawText(float posX, float posY, string text, uint size, Color textColor, Font font)
        {
            Text _text = new Text(text, font.SFMLFont);
            _text.Position = new SFML.System.Vector2f(posX, posY);
            _text.FillColor = textColor.Convert();
            _text.CharacterSize = size;

            CurrentRenderTarget.DrawSFML(_text);
        }
        #endregion Text

        #endregion Drawing Methods

        #region Client Window Data

        /// <summary>
        /// Transforms a point from the world (tile) space, to screen (pixel) space.
        /// </summary>
        public static ScreenCoordinates WorldToScreen(LocalCoordinates point)
        {
            return new ScreenCoordinates(((point.Position - Camera.Position) * Camera.PixelsPerMeter + Window.Viewport.Size / 2), point.MapID);
        }

        public static Vector2 WorldToScreen(Vector2 point)
        {
            var center = Camera.Position;
            return (point - center) * Camera.PixelsPerMeter + Window.Viewport.Size / 2;
        }

        /// <summary>
        /// Transforms a rectangle from the world (tile) space, to screen (pixel) space.
        /// </summary>
        public static Box2 WorldToScreen(Box2 rect)
        {
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
        public static LocalCoordinates ScreenToCoordinates(ScreenCoordinates point)
        {
            // world pos in current map
            var pos = (point.Position - Window.Viewport.Size / 2) / Camera.PixelsPerMeter + Camera.Position;

            // world coords on map
            return new LocalCoordinates(pos, GridId.DefaultGrid, point.MapID);
        }

        /// <summary>
        /// Transforms a rectangle from the screen (pixel) space, to world (tile) space.
        /// </summary>
        public static Box2 ScreenToWorld(Box2i rect)
        {
            var center = Camera.Position;
            return new Box2(
                ((Vector2)rect.TopLeft - Window.Viewport.Size / 2) / Camera.PixelsPerMeter + center,
                ((Vector2)rect.BottomRight - Window.Viewport.Size / 2) / Camera.PixelsPerMeter + center
            );
        }

        /// <summary>
        /// Transforms a point from the screen (pixel) space, to world (tile) space.
        /// </summary>
        public static LocalCoordinates ScreenToWorld(Vector2i point, MapId argMap)
        {
            var pos = ((Vector2)point - Window.Viewport.Size / 2) / Camera.PixelsPerMeter + Camera.Position;
            var grid = IoCManager.Resolve<IMapManager>().GetMap(argMap).FindGridAt(pos);
            return new LocalCoordinates(pos, grid);
        }

        /// <summary>
        /// Scales a vector from pixel coordinates to tile coordinates.
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static Vector2 PixelToTile(Vector2 vec)
        {
            return vec / Camera.PixelsPerMeter;
        }

        #endregion Client Window Data
    }
}
