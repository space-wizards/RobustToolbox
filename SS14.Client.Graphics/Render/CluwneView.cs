using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Window;
using SFML.Graphics;
using SFML.System;
using SS14.Shared.Maths;

namespace SS14.Client.Graphics.Render
{
    /// <summary>
    /// Terminology:
    ///     Pixel Position (SFML: Pixel): the pixel that was clicked physically, this is returned by the SFML MouseButton/MouseMove events
    ///     Window Position: coordinates in original window size (not resized), this is used by the UI
    ///     World Position (SFML: Coords): coordinates used by the gameplay
    /// </summary>
    public class CluwneView
    {
        private CluwneWindow _window;
        private View _worldView;
        private View _interfaceView;

        public Vector2 ViewSize
        {
            get
            {
                return _window.GetView().Size;
            }
        }

        public CluwneView(CluwneWindow window)
        {
            this._window = window;
            _worldView = new View(new FloatRect(0.0f, 0.0f, window.Size.X, window.Size.Y));
            _interfaceView = new View(new FloatRect(0.0f, 0.0f, window.Size.X, window.Size.Y));
            window.SetView(_worldView);
        }

        public void SetWorldView()
        {
            _window.SetView(_worldView);
        }

        public void SetInterfaceView()
        {
            _window.SetView(_interfaceView);
        }

        public void SetWorldCenter(Vector2 newCenter)
        {
            _worldView.Center = CluwneLib.Camera.SnapToPixels(newCenter);
        }

        /// <summary>
        /// Returns event position of MouseButtonEvent where it would be if the window wasn't resized
        /// See Terminology above
        /// </summary>
        public MouseButtonEventArgs EventToWindowPos(MouseButtonEventArgs mouseButtonEvent)
        {
            Vector2i windowPos = (Vector2i)_window.MapPixelToCoords(
                new Vector2i((int)mouseButtonEvent.X, (int)mouseButtonEvent.Y), _interfaceView);
            MouseButtonEvent newMouseEvent = new MouseButtonEvent();
            newMouseEvent.X = windowPos.X;
            newMouseEvent.Y = windowPos.Y;
            newMouseEvent.Button = mouseButtonEvent.Button;
            MouseButtonEventArgs eventArgsWindowPos = new MouseButtonEventArgs(newMouseEvent);
            return eventArgsWindowPos;
        }

        /// <summary>
        /// Returns event position of MouseMoveEvent where it would be if the window wasn't resized
        /// See Terminology above
        /// </summary>
        public MouseMoveEventArgs EventToWindowPos(MouseMoveEventArgs mouseMoveEvent)
        {
            Vector2i windowPos = (Vector2i)_window.MapPixelToCoords(
                new Vector2i((int)mouseMoveEvent.X, (int)mouseMoveEvent.Y), _interfaceView);
            mouseMoveEvent.X = (int)windowPos.X;
            mouseMoveEvent.Y = (int)windowPos.Y;
            MouseMoveEvent newMouseEvent = new MouseMoveEvent();
            newMouseEvent.X = windowPos.X;
            newMouseEvent.Y = windowPos.Y;
            MouseMoveEventArgs eventArgsWindowPos = new MouseMoveEventArgs(newMouseEvent);
            return eventArgsWindowPos;
        }

        /// <summary>
        /// Prevent flickering of tiles even if the window is resized
        /// The real solution would be to render off-screen first then scale that to screen size but this is ok for now
        /// </summary>
        public Vector2 SnapToPixels(Vector2 worldCoords)
        {
            Vector2 origWindowSize = _worldView.Size;
            Vector2 currentSize = (Vector2)_window.Size;
            //Scale to screen and snap
            Vector2 pixelPos = worldCoords * currentSize / origWindowSize;
            pixelPos = pixelPos.Floor();
            //Scale back
            Vector2 worldVect = pixelPos * origWindowSize / currentSize;
            return worldVect;
        }

        /// <summary>
        /// See Terminology above
        /// </summary>
        public Vector2 PixelToWorldPos(Vector2 pixelPos)
        {
            Vector2 worldCoords = CluwneLib.Screen.MapPixelToCoords((Vector2i)pixelPos, _worldView);
            return worldCoords;
        }

        public Vector2 PixelToWindowPos(Vector2 pixelPos)
        {
            Vector2 windowPos = _window.MapPixelToCoords((Vector2i)pixelPos, _interfaceView);
            return windowPos;
        }
    }
}
