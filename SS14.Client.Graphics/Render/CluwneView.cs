using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Window;
using SFML.Graphics;
using SFML.System;

namespace SS14.Client.Graphics.Render
{
    public class CluwneView
    {
        private CluwneWindow _window;
        private View _worldView;
        private View _interfaceView;

        public Vector2f ViewSize
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

        public void SetWorldCenter(float x, float y)
        {
            Vector2f vect = new Vector2f(x, y);
            _worldView.Center = CluwneLib.Camera.SnapToScreenPixels(vect);
        }

        /// <summary>
        /// Sets position of MouseButtonEvent where it would be if the window wasn't resized
        /// </summary>
        public void ScaleEventToOriginalSize(MouseButtonEventArgs mouseButtonEvent)
        {
            Vector2f originalMousePos = _window.MapPixelToCoords(
                new Vector2i((int)mouseButtonEvent.X, (int)mouseButtonEvent.Y), _interfaceView);
            mouseButtonEvent.X = (int)originalMousePos.X;
            mouseButtonEvent.Y = (int)originalMousePos.Y;
        }

        /// <summary>
        /// Sets position of MouseMoveEvent where it would be if the window wasn't resized
        /// </summary>
        public void ScaleEventToOriginalSize(MouseMoveEventArgs mouseMoveEvent)
        {
            Vector2f originalMousePos = _window.MapPixelToCoords(
                new Vector2i((int)mouseMoveEvent.X, (int)mouseMoveEvent.Y), _interfaceView);
            mouseMoveEvent.X = (int)originalMousePos.X;
            mouseMoveEvent.Y = (int)originalMousePos.Y;
        }

        /// <summary>
        /// Prevent flickering of tiles even if the window is resized
        /// The real solution would be to render off-screen first but this is ok for now
        /// </summary>
        public Vector2f SnapToScreenPixels(Vector2f coords)
        {
            float origW = _worldView.Size.X;
            float origH = _worldView.Size.Y;
            float w = _window.Size.X;
            float h = _window.Size.Y;
            //Scale to screen and round
            float screenX = (float)Math.Floor(coords.X * w / origW);
            float screenY = (float)Math.Floor(coords.Y * h / origH);
            //Scale back
            Vector2f retVect = new Vector2f(screenX * origW / w, screenY * origH / h);

            return retVect;
        }
    }
}
