using System.Linq;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.Client.Placement.Modes
{
    public class AlignSimilar : PlacementMode
    {
        private const uint SnapToRange = 50;

        public AlignSimilar(PlacementManager pMan) : base(pMan) { }

        public override bool FrameUpdate(RenderFrameEventArgs e, ScreenCoordinates mouseS)
        {
            if (mouseS.MapID == MapId.Nullspace) return false;

            MouseScreen = mouseS;
            MouseCoords = pManager.eyeManager.ScreenToWorld(MouseScreen);

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
                    var closestSprite = component.CurrentSprite;
                    var closestBounds = closestSprite.Size;

                    var closestRect =
                        Box2.FromDimensions(
                            closestEntity.GetComponent<ITransformComponent>().WorldPosition.X - closestBounds.X / 2f,
                            closestEntity.GetComponent<ITransformComponent>().WorldPosition.Y - closestBounds.Y / 2f,
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

                    MouseCoords = new LocalCoordinates(closestSide, MouseCoords.Grid);
                    MouseScreen = pManager.eyeManager.WorldToScreen(MouseCoords);
                }
            }

            if (CheckCollision())
                return false;
            return true;
        }
    }
}
