using System.Linq;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Robust.Client.Placement.Modes
{
    public sealed class AlignSimilar : PlacementMode
    {
        private const uint SnapToRange = 50;

        public AlignSimilar(PlacementManager pMan) : base(pMan) { }

        public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
        {
            MouseCoords = ScreenToCursorGrid(mouseScreen);
            CurrentTile = GetTileRef(MouseCoords);

            if (pManager.CurrentPermission!.IsTile)
            {
                return;
            }

            if (!RangeCheck(MouseCoords))
            {
                return;
            }

            var mapId = MouseCoords.GetMapId(pManager.EntityManager);

            var snapToEntities = EntitySystem.Get<EntityLookupSystem>().GetEntitiesInRange(MouseCoords, SnapToRange)
                .Where(entity => pManager.EntityManager.GetComponent<MetaDataComponent>(entity).EntityPrototype == pManager.CurrentPrototype && pManager.EntityManager.GetComponent<TransformComponent>(entity).MapID == mapId)
                .OrderBy(entity => (pManager.EntityManager.GetComponent<TransformComponent>(entity).WorldPosition - MouseCoords.ToMapPos(pManager.EntityManager)).LengthSquared)
                .ToList();

            if (snapToEntities.Count == 0)
            {
                return;
            }

            var closestEntity = snapToEntities[0];
            var closestTransform = pManager.EntityManager.GetComponent<TransformComponent>(closestEntity);
            if (!pManager.EntityManager.TryGetComponent<ISpriteComponent?>(closestEntity, out var component) || component.BaseRSI == null)
            {
                return;
            }

            var closestBounds = component.BaseRSI.Size;

            var closestRect =
                Box2.FromDimensions(
                    closestTransform.WorldPosition.X - closestBounds.X / 2f,
                    closestTransform.WorldPosition.Y - closestBounds.Y / 2f,
                    closestBounds.X, closestBounds.Y);

            var sides = new[]
            {
                new Vector2(closestRect.Left + closestRect.Width / 2f, closestRect.Top - closestBounds.Y / 2f),
                new Vector2(closestRect.Left + closestRect.Width / 2f, closestRect.Bottom + closestBounds.Y / 2f),
                new Vector2(closestRect.Left - closestBounds.X / 2f, closestRect.Top + closestRect.Height / 2f),
                new Vector2(closestRect.Right + closestBounds.X / 2f, closestRect.Top + closestRect.Height / 2f)
            };

            var closestSide =
                (from Vector2 side in sides orderby (side - MouseCoords.Position).LengthSquared select side).First();

            MouseCoords = new EntityCoordinates(MouseCoords.EntityId, MouseCoords.Position);
        }

        public override bool IsValidPosition(EntityCoordinates position)
        {
            if (pManager.CurrentPermission!.IsTile)
            {
                return false;
            }

            if (!RangeCheck(position))
            {
                return false;
            }

            return !IsColliding(position);
        }
    }
}
