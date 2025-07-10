using System;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Robust.Client.Placement.Modes
{
    public sealed class SnapgridBorder : SnapgridCenter
    {
        public override bool HasLineMode => true;
        public override bool HasGridMode => true;

        public SnapgridBorder(PlacementManager pMan) : base(pMan)
        {
        }


        public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
        {
            MouseCoords = ScreenToCursorGrid(mouseScreen);

            var transformSys = pManager.EntityManager.System<SharedTransformSystem>();
            var gridIdOpt = transformSys.GetGrid(MouseCoords);
            SnapSize = 1f;
            if (gridIdOpt is EntityUid gridId && gridId.IsValid())
            {
                Grid = pManager.EntityManager.GetComponent<MapGridComponent>(gridId);
                SnapSize = Grid.TileSize; //Find snap size for the grid.
            }
            else
            {
                Grid = null;
            }

            GridDistancing = SnapSize;

            var mouselocal = new Vector2( //Round local coordinates onto the snap grid
                MathF.Round(MouseCoords.X / SnapSize, MidpointRounding.AwayFromZero) * SnapSize,
                MathF.Round(MouseCoords.Y / SnapSize, MidpointRounding.AwayFromZero) * SnapSize);

            //Convert back to original world and screen coordinates after applying offset
            MouseCoords =
                new EntityCoordinates(
                    MouseCoords.EntityId, mouselocal + new Vector2(pManager.PlacementOffset.X, pManager.PlacementOffset.Y));
        }
    }
}
