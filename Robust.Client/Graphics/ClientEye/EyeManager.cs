using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;

#nullable enable

namespace Robust.Client.Graphics
{
    /// <inheritdoc />
    public sealed class EyeManager : IEyeManager
    {
        // If you modify this make sure to edit the value in the Robust.Shared.Audio.AudioParams struct default too!
        // No I can't be bothered to make this a shared constant.
        /// <summary>
        /// Default scaling for the projection matrix.
        /// </summary>
        public const int PixelsPerMeter = 32;

        [Dependency] private readonly IClyde _displayManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
        private ISawmill _logMill = default!;

        // We default to this when we get set to a null eye.
        private readonly FixedEye _defaultEye = new();

        private IEye? _currentEye;

        /// <inheritdoc />
        public IEye CurrentEye
        {
            get => _currentEye ?? _defaultEye;
            set
            {
                var old = _currentEye;
                _currentEye = value;

                _entityManager.EventBus.RaiseEvent(EventSource.Local, new CurrentEyeChangedEvent(old, _currentEye));
            }
        }

        public IViewportControl MainViewport { get; set; } = default!;

        public void ClearCurrentEye()
        {
            CurrentEye = _defaultEye;
        }

        void IEyeManager.Initialize()
        {
            MainViewport = _uiManager.MainViewport;
            _logMill = IoCManager.Resolve<ILogManager>().RootSawmill;
        }

        /// <inheritdoc />
        public MapId CurrentMap => CurrentEye.Position.MapId;

        /// <inheritdoc />
        public Box2 GetWorldViewport()
        {
            return GetWorldViewbounds().CalcBoundingBox();
        }

        /// <inheritdoc />
        public Box2Rotated GetWorldViewbounds()
        {
            // This is an inefficient and roundabout way of geting the viewport.
            // But its a method that shouldn't get used much.

            var vp = MainViewport as Control;
            var vpSize = vp?.PixelSize ?? _displayManager.ScreenSize;

            var topRight = ScreenToMap(new Vector2(vpSize.X, 0)).Position;
            var bottomLeft = ScreenToMap(new Vector2(0, vpSize.Y)).Position;

            // This assumes the main viewports eye and the main eye are the same.
            var rotation = new Angle(CurrentEye.Rotation);
            var center = (bottomLeft + topRight) / 2;

            var localTopRight = topRight - center;
            var localBotLeft = bottomLeft - center;

            localTopRight = rotation.RotateVec(localTopRight);
            localBotLeft = rotation.RotateVec(localBotLeft);

            var bounds = new Box2(localBotLeft, localTopRight).Translated(center);

            return new Box2Rotated(bounds, -CurrentEye.Rotation, bounds.Center);
        }

        /// <inheritdoc />
        public Vector2 WorldToScreen(Vector2 point)
        {
            return MainViewport.WorldToScreen(point);
        }

        /// <inheritdoc />
        public void GetScreenProjectionMatrix(out Matrix3x2 projMatrix)
        {
            Matrix3x2 result = default;

            result.M11 = PixelsPerMeter;
            result.M22 = -PixelsPerMeter;

            var screenSize = _displayManager.ScreenSize;
            result.M31 = screenSize.X / 2f;
            result.M32 = screenSize.Y / 2f;

            /* column major
             Sx 0 Tx
             0 Sy Ty
             0  0  1
            */
            projMatrix = result;
        }

        /// <inheritdoc />
        public ScreenCoordinates CoordinatesToScreen(EntityCoordinates point)
        {
            var transformSystem = _entityManager.System<SharedTransformSystem>();
            return MapToScreen(transformSystem.ToMapCoordinates(point));
        }

        public ScreenCoordinates MapToScreen(MapCoordinates point)
        {
            if (CurrentEye.Position.MapId != point.MapId)
            {
                _logMill.Error($"Attempted to convert map coordinates ({point}) to screen coordinates with an eye on another map ({CurrentEye.Position.MapId})");
                return new(default, WindowId.Invalid);
            }

            return new(WorldToScreen(point.Position), MainViewport.Window?.Id ?? default);
        }

        /// <inheritdoc />
        public MapCoordinates ScreenToMap(ScreenCoordinates point)
        {
            if (_uiManager.MouseGetControl(point) is not IViewportControl viewport)
                return default;

            return viewport.ScreenToMap(point.Position);
        }

        /// <inheritdoc />
        public MapCoordinates ScreenToMap(Vector2 point)
        {
            return MainViewport.ScreenToMap(point);
        }

        /// <inheritdoc />
        public MapCoordinates PixelToMap(ScreenCoordinates point)
        {
            if (_uiManager.MouseGetControl(point) is not IViewportControl viewport)
                return default;

            return viewport.PixelToMap(point.Position);
        }

        /// <inheritdoc />
        public MapCoordinates PixelToMap(Vector2 point)
        {
            return MainViewport.PixelToMap(point);
        }
    }

    public sealed class CurrentEyeChangedEvent : EntityEventArgs
    {
        public IEye? Old { get; }
        public IEye New { get; }

        public CurrentEyeChangedEvent(IEye? oldEye, IEye newEye)
        {
            Old = oldEye;
            New = newEye;
        }
    }
}
