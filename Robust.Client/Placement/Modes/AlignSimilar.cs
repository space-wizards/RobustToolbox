using System.Linq;
using System.Numerics;
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

            var transformSys = pManager.EntityManager.System<SharedTransformSystem>();
            var mapId = transformSys.GetMapId(MouseCoords);

            var snapToEntities = pManager.EntityManager.System<EntityLookupSystem>()
                .GetEntitiesInRange(MouseCoords, SnapToRange)
                .Where(entity => pManager.EntityManager.GetComponent<MetaDataComponent>(entity).EntityPrototype == pManager.CurrentPrototype && pManager.EntityManager.GetComponent<TransformComponent>(entity).MapID == mapId)
                .OrderBy(entity => (transformSys.GetWorldPosition(entity) - transformSys.ToMapCoordinates(MouseCoords).Position).LengthSquared())
                .ToList();

            if (snapToEntities.Count == 0)
            {
                return;
            }

            var closestEntity = snapToEntities[0];
            var closestTransform = pManager.EntityManager.GetComponent<TransformComponent>(closestEntity);
            if (!pManager.EntityManager.TryGetComponent(closestEntity, out SpriteComponent? component) || component.BaseRSI == null)
            {
                return;
            }

            var closestBounds = component.BaseRSI.Size;

            var closestPos = transformSys.GetWorldPosition(closestTransform);
            var closestRect =
                Box2.FromDimensions(
                    closestPos.X - closestBounds.X / 2f,
                    closestPos.Y - closestBounds.Y / 2f,
                    closestBounds.X, closestBounds.Y);

            var sides = new[]
            {
                new Vector2(closestRect.Left + closestRect.Width / 2f, closestRect.Top - closestBounds.Y / 2f),
                new Vector2(closestRect.Left + closestRect.Width / 2f, closestRect.Bottom + closestBounds.Y / 2f),
                new Vector2(closestRect.Left - closestBounds.X / 2f, closestRect.Top + closestRect.Height / 2f),
                new Vector2(closestRect.Right + closestBounds.X / 2f, closestRect.Top + closestRect.Height / 2f)
            };

            var closestSide =
                (from Vector2 side in sides orderby (side - MouseCoords.Position).LengthSquared() select side).First();

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
