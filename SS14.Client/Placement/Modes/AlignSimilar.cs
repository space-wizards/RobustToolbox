using OpenTK;
using SS14.Client.GameObjects;
using SS14.Client.Graphics;
using SS14.Client.Helpers;
using SS14.Client.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using System.Collections.Generic;
using System.Linq;
using SS14.Shared.Utility;
using Vector2i = SS14.Shared.Maths.Vector2i;
using SS14.Shared.Map;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Client.Placement.Modes
{
    public class AlignSimilar : PlacementMode
    {
        private const uint snapToRange = 50;

        public AlignSimilar(PlacementManager pMan) : base(pMan)
        {
        }

        public override bool Update(ScreenCoordinates mouseS)
        {
            if (mouseS.MapID == MapManager.NULLSPACE) return false;

            MouseScreen = mouseS;
            MouseCoords = CluwneLib.ScreenToCoordinates(MouseScreen);

            if (pManager.CurrentPermission.IsTile)
                return false;

            CurrentTile = MouseCoords.Grid.GetTile(MouseCoords);

            if (!RangeCheck())
                return false;

            var manager = IoCManager.Resolve<IClientEntityManager>();

            IOrderedEnumerable<IEntity> snapToEntities =
                from IEntity entity in manager.GetEntitiesInRange(MouseCoords, snapToRange)
                where entity.Prototype == pManager.CurrentPrototype &&
                      entity.GetComponent<ITransformComponent>().MapID == MouseCoords.MapID
                orderby
                    (entity.GetComponent<ITransformComponent>(
                        ).WorldPosition - MouseCoords.ToWorld().Position).LengthSquared
                    ascending
                select entity;

            if (snapToEntities.Any())
            {
                IEntity closestEntity = snapToEntities.First();
                if (closestEntity.TryGetComponent<ISpriteRenderableComponent>(out var component))
                {
                    var closestSprite = component.GetCurrentSprite();
                    var closestBounds = closestSprite.LocalBounds;

                    var closestRect =
                        Box2.FromDimensions(
                            closestEntity.GetComponent<ITransformComponent>().WorldPosition.X - closestBounds.Width / 2f,
                            closestEntity.GetComponent<ITransformComponent>().WorldPosition.Y - closestBounds.Height / 2f,
                            closestBounds.Width, closestBounds.Height);

                    var sides = new Vector2[]
                    {
                        new Vector2(closestRect.Left + (closestRect.Width / 2f), closestRect.Top - closestBounds.Height / 2f),
                        new Vector2(closestRect.Left + (closestRect.Width / 2f), closestRect.Bottom + closestBounds.Height / 2f),
                        new Vector2(closestRect.Left - closestBounds.Width / 2f, closestRect.Top + (closestRect.Height / 2f)),
                        new Vector2(closestRect.Right + closestBounds.Width / 2f, closestRect.Top + (closestRect.Height / 2f))
                    };

                    Vector2 closestSide =
                        (from Vector2 side in sides orderby (side - MouseCoords.Position).LengthSquared ascending select side).First();

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
