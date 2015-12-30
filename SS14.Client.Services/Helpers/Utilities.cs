using SFML.Graphics;
using SS14.Client.GameObjects;
using SS14.Client.Graphics.Sprite;
using SS14.Client.Interfaces.Map;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using System;
using System.Drawing;
using Image = SFML.Graphics.Image;
using Color = SFML.Graphics.Color;

namespace SS14.Client.Services.Helpers
{
    internal static class Utilities
    {
        public static string GetObjectSpriteName(Type type)
        {
            return type.IsSubclassOf(typeof (ITileDefinition)) ? "tilebuildoverlay" : "nosprite";
        }

        public static Sprite GetSpriteComponentSprite(Entity entity)
        {
            ComponentReplyMessage reply = entity.SendMessage(entity, ComponentFamily.Renderable,
                                                             ComponentMessageType.GetSprite);
            if (reply.MessageType == ComponentMessageType.CurrentSprite)
            {
                var sprite = (Sprite) reply.ParamsList[0];
                return sprite;
            }
            return null;
        }

        public static Sprite GetIconSprite(Entity entity)
        {
            if(entity.HasComponent(ComponentFamily.Icon))
            {
                var icon = entity.GetComponent<IconComponent>(ComponentFamily.Icon).Icon;
                if (icon == null)
                    return IoCManager.Resolve<IResourceManager>().GetNoSprite();
                return icon;
            }
            return IoCManager.Resolve<IResourceManager>().GetNoSprite();
        }

        public static bool SpritePixelHit(Sprite toCheck, Vector2 clickPos)
        {
            var clickPoint = new PointF(clickPos.X, clickPos.Y);
            if (!toCheck.GetLocalBounds().Contains(clickPoint.X, clickPoint.Y)) return false;

            var spritePosition = new Point((int) clickPos.X - (int) toCheck.Position.X ,//+ (int) toCheck.ImageOffset.X,
                                           (int) clickPos.Y - (int) toCheck.Position.Y ); //+ (int) toCheck.ImageOffset.Y);

            Image imgData = toCheck.Texture.CopyToImage();

            //imgData.Lock(false);
            Color pixColour = imgData.GetPixel((uint)spritePosition.X,(uint) spritePosition.Y);
            imgData.Dispose();
            //imgData.Unlock();

            return pixColour.A != 0;
        }
    }
}