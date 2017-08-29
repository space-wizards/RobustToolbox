using OpenTK;
using SFML.Graphics;
using SFML.System;
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
            if (currentMap == null) return false;
            
            mouseScreen = mouseS;
            mouseWorld = CluwneLib.ScreenToWorld(mouseScreen);

            if (pManager.CurrentPermission.IsTile)
                return false;

            currentTile = currentMap.GetDefaultGrid().GetTile(mouseWorld);

            if (!RangeCheck())
                return false;

            var manager = IoCManager.Resolve<IClientEntityManager>();

            IOrderedEnumerable<IEntity> snapToEntities =
                from IEntity entity in manager.GetEntitiesInRange(mouseWorld, snapToRange)
                where entity.Prototype == pManager.CurrentPrototype
                orderby
                    (entity.GetComponent<ITransformComponent>(
                        ).Position - mouseWorld).LengthSquared
                    ascending
                select entity;

            if (snapToEntities.Any())
            {
                IEntity closestEntity = snapToEntities.First();
                if (closestEntity.TryGetComponent<ISpriteRenderableComponent>(out var component))
                {
                    var closestSprite = component.GetCurrentSprite();
                    var closestBounds = closestSprite.GetLocalBounds();

                    var closestRect =
                        Box2.FromDimensions(
                            closestEntity.GetComponent<ITransformComponent>().Position.X - closestBounds.Width / 2f,
                            closestEntity.GetComponent<ITransformComponent>().Position.Y - closestBounds.Height / 2f,
                            closestBounds.Width, closestBounds.Height);

                    var sides = new Vector2[]
                    {
                        new Vector2(closestRect.Left + (closestRect.Width / 2f), closestRect.Top - closestBounds.Height / 2f),
                        new Vector2(closestRect.Left + (closestRect.Width / 2f), closestRect.Bottom + closestBounds.Height / 2f),
                        new Vector2(closestRect.Left - closestBounds.Width / 2f, closestRect.Top + (closestRect.Height / 2f)),
                        new Vector2(closestRect.Right + closestBounds.Width / 2f, closestRect.Top + (closestRect.Height / 2f))
                    };

                    Vector2 closestSide =
                        (from Vector2 side in sides orderby (side - mouseWorld).LengthSquared ascending select side).First();

                    mouseWorld = closestSide;
                    mouseScreen = (Vector2i)CluwneLib.WorldToScreen(mouseWorld);
                }
            }

            if (CheckCollision())
                return false;
            return true;
        }
    }
}
