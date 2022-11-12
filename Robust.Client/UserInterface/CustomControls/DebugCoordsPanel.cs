using System.Text;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.CustomControls
{
    internal sealed class DebugCoordsPanel : PanelContainer
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IClyde _displayManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        private readonly StringBuilder _textBuilder = new();
        private readonly char[] _textBuffer = new char[1024];

        private readonly Label _contents;

        public DebugCoordsPanel()
        {
            IoCManager.InjectDependencies(this);

            HorizontalAlignment = HAlignment.Left;

            _contents = new Label
            {
                FontColorShadowOverride = Color.Black
            };
            AddChild(_contents);

            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = new Color(67, 105, 255, 138),
                ContentMarginLeftOverride = 5,
                ContentMarginTopOverride = 5
            };

            MouseFilter = _contents.MouseFilter = MouseFilterMode.Ignore;
            MinWidth = 175;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            if (!VisibleInTree)
            {
                return;
            }

            _textBuilder.Clear();

            var mouseScreenPos = _inputManager.MouseScreenPosition;
            var screenSize = _displayManager.ScreenSize;

            MapCoordinates mouseWorldMap;
            EntityCoordinates mouseGridPos;
            TileRef tile;

            mouseWorldMap = _eyeManager.ScreenToMap(mouseScreenPos);

            if (_mapManager.TryFindGridAt(mouseWorldMap, out var mouseGrid))
            {
                mouseGridPos = mouseGrid.MapToGrid(mouseWorldMap);
                tile = mouseGrid.GetTileRef(mouseGridPos);
            }
            else
            {
                mouseGridPos = new EntityCoordinates(_mapManager.GetMapEntityId(mouseWorldMap.MapId),
                    mouseWorldMap.Position);
                tile = new TileRef(EntityUid.Invalid, mouseGridPos.ToVector2i(_entityManager, _mapManager), Tile.Empty);
            }

            var controlHovered = UserInterfaceManager.CurrentlyHovered;

            _textBuilder.Append($@"Positioning Debug:
Screen Size: {screenSize}
Mouse Pos:
    Screen: {mouseScreenPos}
    {mouseWorldMap}
    {mouseGridPos}
    {tile}
    GUI: {controlHovered}");

            _textBuilder.AppendLine("\nAttached Entity:");
            var controlledEntity = _playerManager?.LocalPlayer?.ControlledEntity ?? EntityUid.Invalid;
            if (controlledEntity == EntityUid.Invalid)
            {
                _textBuilder.AppendLine("No attached entity.");
            }
            else
            {
                var entityTransform = _entityManager.GetComponent<TransformComponent>(controlledEntity);
                var playerWorldOffset = entityTransform.MapPosition;
                var playerScreen = _eyeManager.WorldToScreen(playerWorldOffset.Position);

                var playerCoordinates = entityTransform.Coordinates;
                var playerRotation = entityTransform.WorldRotation;
                var gridRotation = entityTransform.GridUid != null
                    ? _entityManager.GetComponent<TransformComponent>(entityTransform.GridUid.Value)
                    .WorldRotation
                    : Angle.Zero;

                _textBuilder.Append($@"    Screen: {playerScreen}
    {playerWorldOffset}
    {playerCoordinates}
    Rotation: {playerRotation.Degrees:F2}°
    EntId: {entityTransform.Owner}
    GridUid: {entityTransform.GridUid}
    Grid Rotation: {gridRotation.Degrees:F2}°");
            }

            _contents.TextMemory = FormatHelpers.BuilderToMemory(_textBuilder, _textBuffer);
        }
    }
}
