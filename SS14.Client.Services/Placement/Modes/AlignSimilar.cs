using SFML.Graphics;
using SS14.Client.GameObjects;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.GOC;
using SS14.Client.Interfaces.Map;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using EntityManager = SS14.Client.GameObjects.EntityManager;
using Color = SFML.Graphics.Color;

namespace SS14.Client.Services.Placement.Modes
{
    public class AlignSimilar : PlacementMode
    {
        private const uint snapToRange = 50;

        public AlignSimilar(PlacementManager pMan)
            : base(pMan)
        {
        }

        public override bool Update(Vector2 mouseS, IMapManager currentMap)
        {
            if (currentMap == null) return false;

            spriteToDraw = GetDirectionalSprite(pManager.CurrentBaseSpriteKey);
            var spriteBounds = spriteToDraw.GetLocalBounds();

            mouseScreen = mouseS;
            mouseWorld = CluwneLib.ScreenToWorld(mouseScreen);

            var spriteSize = CluwneLib.PixelToTile(new PointF(spriteBounds.Width, spriteBounds.Height)); // TODO: Doublecheck this.  Use SizeF?
            var spriteRectWorld = new RectangleF(mouseWorld.X - (spriteSize.X / 2f),
                                                 mouseWorld.Y - (spriteSize.Y / 2f),
                                                 spriteSize.X, spriteSize.Y);

            if (pManager.CurrentPermission.IsTile)
                return false;

            currentTile = currentMap.GetTileRef(mouseWorld);

            //Align to similar if nearby found else free
            if (currentTile.Tile.TileDef.IsWall)
                return false; //HANDLE CURSOR OUTSIDE MAP

            if (pManager.CurrentPermission.Range > 0)
                if (
                    (pManager.PlayerManager.ControlledEntity.GetComponent<TransformComponent>(ComponentFamily.Transform)
                         .Position - mouseWorld).Length > pManager.CurrentPermission.Range) return false;

            Entity[] nearbyEntities =
                ((EntityManager) IoCManager.Resolve<IEntityManagerContainer>().EntityManager).GetEntitiesInRange(
                    mouseWorld, snapToRange);

            IOrderedEnumerable<Entity> snapToEntities = from Entity entity in nearbyEntities
                                                        where entity.Template == pManager.CurrentTemplate
                                                        orderby
                                                            (entity.GetComponent<TransformComponent>(
                                                                ComponentFamily.Transform).Position - mouseWorld).Length
                                                            ascending
                                                        select entity;

            if (snapToEntities.Any())
            {
                Entity closestEntity = snapToEntities.First();
                ComponentReplyMessage reply = closestEntity.SendMessage(this, ComponentFamily.Renderable,
                                                                        ComponentMessageType.GetSprite);

                //if(replies.Any(x => x.messageType == SS13_Shared.GO.ComponentMessageType.CurrentSprite))
                //{
                //    Sprite closestSprite = (Sprite)replies.Find(x => x.messageType == SS13_Shared.GO.ComponentMessageType.CurrentSprite).paramsList[0]; //This is safer but slower.

                if (reply.MessageType == ComponentMessageType.CurrentSprite)
                {
                    var closestSprite = (Sprite) reply.ParamsList[0]; //This is faster but kinda unsafe.
                    var closestBounds = closestSprite.GetLocalBounds();

                    var closestRect =
                        new RectangleF(
                            closestEntity.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.X - closestBounds.Width / 2f,
                            closestEntity.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.Y - closestBounds.Height / 2f,
                            closestBounds.Width, closestBounds.Height);

                    var sides = new List<Vector2>
                    {
                        new Vector2(closestRect.X + (closestRect.Width / 2f), closestRect.Top - closestBounds.Height / 2f),
                        new Vector2(closestRect.X + (closestRect.Width / 2f), closestRect.Bottom + closestBounds.Height / 2f),
                        new Vector2(closestRect.Left - closestBounds.Width / 2f, closestRect.Y + (closestRect.Height / 2f)),
                        new Vector2(closestRect.Right + closestBounds.Width / 2f, closestRect.Y + (closestRect.Height / 2f))
                    };

                    Vector2 closestSide =
                        (from Vector2 side in sides orderby (side - mouseWorld).Length ascending select side).First();

                    mouseWorld = closestSide;
                    mouseScreen = CluwneLib.WorldToScreen(mouseWorld);
                }
            }

            spriteRectWorld = new RectangleF(mouseWorld.X - (spriteBounds.Width/2f), mouseWorld.Y - (spriteBounds.Height/2f),
                                             spriteBounds.Width, spriteBounds.Height);
            if (pManager.CollisionManager.IsColliding(spriteRectWorld)) return false;
            return true;
        }

        public override void Render()
        {
            if (spriteToDraw != null)
            {
                var spriteBounds = spriteToDraw.GetLocalBounds();
                spriteToDraw.Color = pManager.ValidPosition ? new Color(0, 128, 0, 255) : new Color(128, 0, 0, 255);
                spriteToDraw.Position = new Vector2(mouseScreen.X - (spriteBounds.Width/2f),
                                                     mouseScreen.Y - (spriteBounds.Height/2f));
                //Centering the sprite on the cursor.
                spriteToDraw.Draw(CluwneLib.CurrentRenderTarget, RenderStates.Default);
                spriteToDraw.Color = Color.White;
            }
        }
    }
}