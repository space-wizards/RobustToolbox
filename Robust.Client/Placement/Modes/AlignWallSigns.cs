using System.Linq;
using Robust.Shared.Map;
using Vector2 = System.Numerics.Vector2;

namespace Robust.Client.Placement.Modes
{
    /// <summary>
    ///     Used for snapping the directional signs to walls.
    /// </summary>
    public sealed class AlignWallSigns : PlacementMode
    {
        public AlignWallSigns(PlacementManager pMan) : base(pMan)
        {
        }

        public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
        {
            MouseCoords = ScreenToCursorGrid(mouseScreen);
            CurrentTile = GetTileRef(MouseCoords);

            if (pManager.CurrentPermission!.IsTile)
            {
                return;
            }

            var tileCoordinates = new EntityCoordinates(MouseCoords.EntityId, CurrentTile.GridIndices);

            var offsets = new Vector2[]
            {
                new(0.5f, 0.5f),
                new(0.5f, 0.25f),
                new(0.5f, 0.75f),
            };

            var closestNode = offsets
                .Select(o => tileCoordinates.Offset(o))
                .MinBy(node => node.TryDistance(pManager.EntityManager, MouseCoords, out var distance) ?
                    distance :
                    (float?) null);

            MouseCoords = closestNode;
        }

        public override bool IsValidPosition(EntityCoordinates position)
        {
            if (pManager.CurrentPermission!.IsTile)
            {
                return false;
            }
            else if (!RangeCheck(position))
            {
                return false;
            }

            return true;
        }
    }
}
