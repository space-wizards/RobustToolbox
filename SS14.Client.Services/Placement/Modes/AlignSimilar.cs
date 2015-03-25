using SS14.Client.Graphics.CluwneLib;
using SS14.Shared.Maths;
using SS14.Client.ClientWindow;
using SS14.Client.GameObjects;
using SS14.Client.Interfaces.GOC;
using SS14.Client.Interfaces.Map;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.IoC;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using EntityManager = SS14.Client.GameObjects.EntityManager;
using SFML.Graphics;
using SS14.Client.Graphics.CluwneLib.Sprite;
using Color = System.Drawing.Color;

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

            spriteToDraw = GetDirectionalSprite(pManager.CurrentBaseSprite);

            mouseScreen = mouseS;
            mouseWorld = new Vector2(mouseScreen.X + ClientWindowData.Singleton.ScreenOrigin.X,
                                      mouseScreen.Y + ClientWindowData.Singleton.ScreenOrigin.Y);

            var spriteRectWorld = new RectangleF(mouseWorld.X - (spriteToDraw.Width/2f),
                                                 mouseWorld.Y - (spriteToDraw.Height/2f), spriteToDraw.Width,
                                                 spriteToDraw.Height);

            if (pManager.CurrentPermission.IsTile)
                return false;

            //Align to similar if nearby found else free
            if (currentMap.IsSolidTile(mouseScreen))
                return false; //HANDLE CURSOR OUTSIDE MAP

            currentTile = currentMap.GetFloorAt(mouseWorld);

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
                    var closestSprite = (CluwneSprite) reply.ParamsList[0]; //This is faster but kinda unsafe.

                    var closestRect =
                        new RectangleF(
                            closestEntity.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.X -
                            closestSprite.Size.X/2f,
                            closestEntity.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.Y -
                            closestSprite.Size.Y/2f, closestSprite.Width, closestSprite.Height);

                    var sides = new List<Vector2>
                                    {
                                        new Vector2(closestRect.X + (closestRect.Width/2f),
                                                     closestRect.Top - spriteToDraw.Height/2f),
                                        new Vector2(closestRect.X + (closestRect.Width/2f),
                                                     closestRect.Bottom + spriteToDraw.Height/2f),
                                        new Vector2(closestRect.Left - spriteToDraw.Width/2f,
                                                     closestRect.Y + (closestRect.Height/2f)),
                                        new Vector2(closestRect.Right + spriteToDraw.Width/2f,
                                                     closestRect.Y + (closestRect.Height/2f))
                                    };

                    Vector2 closestSide =
                        (from Vector2 side in sides orderby (side - mouseWorld).Length ascending select side).First();

                    mouseWorld = closestSide;
                    mouseScreen = new Vector2(closestSide.X - ClientWindowData.Singleton.ScreenOrigin.X,
                                               closestSide.Y - ClientWindowData.Singleton.ScreenOrigin.Y);
                }
            }

            spriteRectWorld = new RectangleF(mouseWorld.X - (spriteToDraw.Width/2f),
                                             mouseWorld.Y - (spriteToDraw.Height/2f), spriteToDraw.Width,
                                             spriteToDraw.Height);
            if (pManager.CollisionManager.IsColliding(spriteRectWorld)) return false;
            return true;
        }

        public override void Render()
        {
            if (spriteToDraw != null)
            {
                spriteToDraw.Color = pManager.ValidPosition ? CluwneLib.SystemColorToSFML( Color.ForestGreen) : CluwneLib.SystemColorToSFML( Color.IndianRed);
                spriteToDraw.Position = new Vector2(mouseScreen.X - (spriteToDraw.Width/2f),
                                                     mouseScreen.Y - (spriteToDraw.Height/2f));
                //Centering the sprite on the cursor.
                spriteToDraw.Draw();
                spriteToDraw.Color = CluwneLib.SystemColorToSFML(Color.White);
            }
        }
    }
}