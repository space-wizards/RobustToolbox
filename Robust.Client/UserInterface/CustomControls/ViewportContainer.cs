using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Maths;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Robust.Client.UserInterface.CustomControls
{
    /// <summary>
    ///     A viewport container shows a viewport.
    /// </summary>
    public class ViewportContainer : Control, IViewportControl
    {
        private readonly IClyde _displayManager;
        private readonly IInputManager _inputManager;

        public IClydeViewport? Viewport { get; set; }

        private Vector2 _viewportResolution = (1f, 1f);

        /// <summary>
        ///     This controls the render target size, *as a fraction of the control size.*
        ///     Combined with controlling the Eye, this allows downscaling the game.
        /// </summary>
        public Vector2 ViewportResolution
        {
            get => _viewportResolution;
            set
            {
                _viewportResolution = value;
                Resized();
            }
        }

        public ViewportContainer()
        {
            _displayManager = IoCManager.Resolve<IClyde>();
            _inputManager = IoCManager.Resolve<IInputManager>();
            MouseFilter = MouseFilterMode.Stop;
            Resized();
        }

        protected internal override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            base.KeyBindDown(args);

            if (args.Handled)
                return;

            _inputManager.ViewportKeyEvent(this, args);
        }

        protected internal override void KeyBindUp(GUIBoundKeyEventArgs args)
        {
            base.KeyBindUp(args);

            if (args.Handled)
                return;

            _inputManager.ViewportKeyEvent(this, args);
        }

        // -- Handlers: Out --

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            if (Viewport == null)
            {
                handle.DrawRect(UIBox2.FromDimensions((0, 0), Size * UserInterfaceManager.UIScale), Color.Red);
            }
            else
            {
                Viewport.RenderScreenOverlaysBelow(handle);

                Viewport.Render();
                handle.DrawTextureRect(Viewport.RenderTarget.Texture,
                    UIBox2.FromDimensions((0, 0), (Vector2i) (Viewport.Size / _viewportResolution)));

                Viewport.RenderScreenOverlaysAbove(handle);
            }
        }

        protected sealed override void Resized()
        {
            Viewport?.Dispose();
            Viewport = _displayManager.CreateViewport(
                Vector2i.ComponentMax((1, 1), (Vector2i) (PixelSize * _viewportResolution)),
                "ViewportContainerViewport");
        }

        // -- Handlers: In --

        // -- Utils / S2M-M2S Base --

        public MapCoordinates LocalPixelToMap(Vector2 point)
        {
            if (Viewport?.Eye == null)
                return MapCoordinates.Nullspace;

            var eye = Viewport.Eye;
            var newPoint = point;

            // pre-scaler
            newPoint *= _viewportResolution;

            // (inlined version of UiProjMatrix^-1)
            newPoint -= Viewport.Size / 2f;
            newPoint *= new Vector2(1, -1) / EyeManager.PixelsPerMeter;

            // view matrix
            eye.GetViewMatrixInv(out var viewMatrixInv);
            newPoint = viewMatrixInv * newPoint;

            return new MapCoordinates(newPoint, eye.Position.MapId);
        }

        public Vector2 WorldToLocalPixel(Vector2 point)
        {
            if (Viewport?.Eye == null)
                return Vector2.Zero;

            var newPoint = Viewport.WorldToLocal(point);

            // pre-scaler
            newPoint /= _viewportResolution;

            return newPoint;
        }

        // -- Utils / S2M-M2S Extended --

        public MapCoordinates ScreenToMap(Vector2 point)
        {
            return LocalPixelToMap(point - GlobalPixelPosition);
        }

        public Vector2 WorldToScreen(Vector2 point)
        {
            return WorldToLocalPixel(point) + GlobalPixelPosition;
        }
    }
}
