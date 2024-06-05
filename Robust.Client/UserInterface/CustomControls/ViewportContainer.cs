using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Robust.Client.UserInterface.CustomControls
{
    /// <summary>
    ///     A viewport container shows a viewport.
    /// </summary>
    [Virtual]
    public class ViewportContainer : Control, IViewportControl
    {
        [Dependency] private readonly IClyde _displayManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;

        public IClydeViewport? Viewport { get; set; }

        private Vector2 _viewportResolution = new Vector2(1f, 1f);

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
            IoCManager.InjectDependencies(this);
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
                handle.DrawRect(UIBox2.FromDimensions(new Vector2(0, 0), Size * UIScale), Color.Red);
            }
            else
            {
                if (Viewport.Eye == null)
                    return;

                var viewportBounds = UIBox2i.FromDimensions(GlobalPixelPosition, PixelSize);
                Viewport.RenderScreenOverlaysBelow(handle, this, viewportBounds);

                Viewport.Render();
                handle.DrawTextureRect(Viewport.RenderTarget.Texture,
                    UIBox2.FromDimensions(new Vector2(0, 0), (Vector2i) (Viewport.Size / _viewportResolution)));

                Viewport.RenderScreenOverlaysAbove(handle, this, viewportBounds);
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
        public MapCoordinates LocalCoordsToMap(Vector2 point)
        {
            if (Viewport == null)
                return default;

            // pre-scaler
            point *= _viewportResolution;

            return Viewport.LocalToWorld(point);
        }

        public MapCoordinates LocalPixelToMap(Vector2 point)
        {
            if (Viewport == null)
                return default;

            // pre-scaler
            point *= _viewportResolution;
            var ev = new PixelToMapEvent(point, this, Viewport);
            _entityManager.EventBus.RaiseEvent(EventSource.Local, ref ev);

            return Viewport.LocalToWorld(ev.VisiblePosition);
        }

        public Vector2 WorldToLocalPixel(Vector2 point)
        {
            if (Viewport?.Eye == null)
                return default;

            var newPoint = Viewport.WorldToLocal(point);

            // pre-scaler
            newPoint /= _viewportResolution;

            return newPoint;
        }

        // -- Utils / S2M-M2S Extended --

        public MapCoordinates ScreenToMap(Vector2 point)
        {
            return LocalCoordsToMap(point - GlobalPixelPosition);
        }

        /// <inheritdoc/>
        public MapCoordinates PixelToMap(Vector2 point)
        {
            return LocalPixelToMap(point - GlobalPixelPosition);
        }

        public Vector2 WorldToScreen(Vector2 point)
        {
            return WorldToLocalPixel(point) + GlobalPixelPosition;
        }

        public Matrix3x2 GetWorldToScreenMatrix()
        {
            if (Viewport == null)
                return Matrix3x2.Identity;

            return Viewport.GetWorldToLocalMatrix() * GetLocalToScreenMatrix();
        }

        public Matrix3x2 GetLocalToScreenMatrix()
        {
            return Matrix3Helpers.CreateTransform(GlobalPixelPosition, 0, Vector2.One / _viewportResolution);
        }
    }
}
