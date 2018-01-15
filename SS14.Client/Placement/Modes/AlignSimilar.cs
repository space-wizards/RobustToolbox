using System.Linq;
using OpenTK;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Client.Placement.Modes
{
    public class AlignSimilar : PlacementMode
    {
        private const uint SnapToRange = 50;

        public AlignSimilar(PlacementManager pMan) : base(pMan) { }

        public override bool Update(ScreenCoordinates mouseS)
        {
            if (mouseS.MapID == MapId.Nullspace) return false;

            MouseScreen = mouseS;
            MouseCoords = CluwneLib.ScreenToCoordinates(MouseScreen);

            if (pManager.CurrentPermission.IsTile)
                return false;

            CurrentTile = MouseCoords.Grid.GetTile(MouseCoords);

            if (!RangeCheck())
                return false;

            var manager = IoCManager.Resolve<IClientEntityManager>();

            var snapToEntities = manager.GetEntitiesInRange(MouseCoords, SnapToRange)
                .Where(entity => entity.Prototype == pManager.CurrentPrototype && entity.GetComponent<ITransformComponent>().MapID == MouseCoords.MapID)
                .OrderBy(entity => (entity.GetComponent<ITransformComponent>().WorldPosition - MouseCoords.ToWorld().Position).LengthSquared)
                .ToList();

            if (snapToEntities.Any())
            {
                var closestEntity = snapToEntities.First();
                if (closestEntity.TryGetComponent<ISpriteRenderableComponent>(out var component))
                {
                    var closestSprite = component.GetCurrentSprite();
                    var closestBounds = closestSprite.LocalBounds;

                    var closestRect =
                        Box2.FromDimensions(
                            closestEntity.GetComponent<ITransformComponent>().WorldPosition.X - closestBounds.Width / 2f,
                            closestEntity.GetComponent<ITransformComponent>().WorldPosition.Y - closestBounds.Height / 2f,
                            closestBounds.Width, closestBounds.Height);

                    var sides = new[]
                    {
                        new Vector2(closestRect.Left + closestRect.Width / 2f, closestRect.Top - closestBounds.Height / 2f),
                        new Vector2(closestRect.Left + closestRect.Width / 2f, closestRect.Bottom + closestBounds.Height / 2f),
                        new Vector2(closestRect.Left - closestBounds.Width / 2f, closestRect.Top + closestRect.Height / 2f),
                        new Vector2(closestRect.Right + closestBounds.Width / 2f, closestRect.Top + closestRect.Height / 2f)
                    };

                    var closestSide =
                        (from Vector2 side in sides orderby (side - MouseCoords.Position).LengthSquared select side).First();

                    MouseCoords = new LocalCoordinates(closestSide, MouseCoords.Grid);
                    MouseScreen = CluwneLib.WorldToScreen(MouseCoords);
                }
            }

            if (CheckCollision())
                return false;
            return true;
        }
    }
}
