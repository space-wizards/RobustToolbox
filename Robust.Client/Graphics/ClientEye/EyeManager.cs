using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics.ClientEye
{
    public sealed class EyeManager : IEyeManager
    {
        // If you modify this make sure to edit the value in the Robust.Shared.Audio.AudioParams struct default too!
        // No I can't be bothered to make this a shared constant.
        public const int PIXELSPERMETER = 32;

#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IClyde _displayManager;
#pragma warning restore 649

        // We default to this when we get set to a null eye.
        private FixedEye defaultEye;

        private IEye currentEye;

        /// <inheritdoc />
        public IEye CurrentEye
        {
            get => currentEye;
            set
            {
                if (currentEye == value)
                {
                    return;
                }

                currentEye = value ?? defaultEye;
            }
        }

        public MapId CurrentMap => currentEye.Position.MapId;

        public Box2 GetWorldViewport()
        {
            var vpSize = _displayManager.ScreenSize;

            var topLeft = ScreenToWorld(Vector2.Zero);
            var topRight = ScreenToWorld(new Vector2(vpSize.X, 0));
            var bottomRight = ScreenToWorld(vpSize);
            var bottomLeft = ScreenToWorld(new Vector2(0, vpSize.Y));

            var left = MathHelper.Min(topLeft.X, topRight.X, bottomRight.X, bottomLeft.X);
            var bottom = MathHelper.Min(topLeft.Y, topRight.Y, bottomRight.Y, bottomLeft.Y);
            var right = MathHelper.Max(topLeft.X, topRight.X, bottomRight.X, bottomLeft.X);
            var top = MathHelper.Max(topLeft.Y, topRight.Y, bottomRight.Y, bottomLeft.Y);

            return new Box2(left, bottom, right, top);
        }

        public void Initialize()
        {
            defaultEye = new FixedEye();
            currentEye = defaultEye;
        }

        public Vector2 WorldToScreen(Vector2 point)
        {
            var matrix = CurrentEye.GetViewMatrix();
            point = matrix.Transform(point);
            point *= new Vector2(1, -1) * PIXELSPERMETER;
            point += _displayManager.ScreenSize/2f;
            return point;
        }

        public ScreenCoordinates WorldToScreen(GridCoordinates point)
        {
            var worldCoords = _mapManager.GetGrid(point.GridID).LocalToWorld(point);
            return new ScreenCoordinates(WorldToScreen(worldCoords.Position));
        }

        public GridCoordinates ScreenToWorld(ScreenCoordinates point)
        {
            return ScreenToWorld(point.Position);
        }

        public GridCoordinates ScreenToWorld(Vector2 point)
        {
            var mapCoords = ScreenToMap(point);

            if (!_mapManager.TryFindGridAt(mapCoords, out var grid))
            {
                grid = _mapManager.GetDefaultGrid(mapCoords.MapId);
            }

            return new GridCoordinates(grid.WorldToLocal(mapCoords.Position), grid.Index);
        }

        public MapCoordinates ScreenToMap(Vector2 point)
        {
            var matrix = Matrix3.Invert(CurrentEye.GetViewMatrix());
            point -= _displayManager.ScreenSize / 2f;
            var worldPos = matrix.Transform(point / PIXELSPERMETER * new Vector2(1, -1));
            return new MapCoordinates(worldPos, CurrentMap);
        }
    }
}
