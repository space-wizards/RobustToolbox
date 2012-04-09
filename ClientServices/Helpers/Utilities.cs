using System;
using System.Collections.Generic;
using System.Linq;
using ClientInterfaces.GOC;
using ClientServices.Map.Tiles;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using SS13_Shared;
using SS13_Shared.GO;
using System.Drawing;

namespace ClientServices.Helpers
{
    static class Utilities
    {
        public static string GetObjectSpriteName(Type type)
        {
            return type.IsSubclassOf(typeof(Tile)) ? "tilebuildoverlay" : "nosprite";
        }

        public static Sprite GetSpriteComponentSprite(IEntity entity)
        {
            var reply = entity.SendMessage(entity, ComponentFamily.Renderable, ComponentMessageType.GetSprite);
            if (reply.MessageType == ComponentMessageType.CurrentSprite)
            {
                var sprite = (Sprite)reply.ParamsList[0];
                return sprite;
            }
            return null;
        }

        public static bool SpritePixelHit(Sprite toCheck, Vector2D clickPos)
        {
            var clickPoint = new PointF(clickPos.X, clickPos.Y);
            if (!toCheck.AABB.Contains(clickPoint)) return false;

            var spritePosition = new Point((int)clickPos.X - (int)toCheck.Position.X + (int)toCheck.ImageOffset.X, (int)clickPos.Y - (int)toCheck.Position.Y + (int)toCheck.ImageOffset.Y);

            var imgData = toCheck.Image.GetImageData();

            imgData.Lock(false);
            var pixColour = Color.FromArgb((int)(imgData[spritePosition.X, spritePosition.Y]));
            imgData.Dispose();
            imgData.Unlock();

            return pixColour.A != 0;
        } 
    }

    public class ColorInterpolator
    {
        delegate byte ComponentSelector(Color color);
        static readonly ComponentSelector RedSelector = color => color.R;
        static readonly ComponentSelector GreenSelector = color => color.G;
        static readonly ComponentSelector BlueSelector = color => color.B;

        public static Color InterpolateBetween(
            Color endPoint1,
            Color endPoint2,
            double lambda)
        {
            if (lambda < 0 || lambda > 1)
            {
                throw new ArgumentOutOfRangeException("lambda");
            }
            var color = Color.FromArgb(
                InterpolateComponent(endPoint1, endPoint2, lambda, RedSelector),
                InterpolateComponent(endPoint1, endPoint2, lambda, GreenSelector),
                InterpolateComponent(endPoint1, endPoint2, lambda, BlueSelector)
                );

            return color;
        }

        static byte InterpolateComponent(
            Color endPoint1,
            Color endPoint2,
            double lambda,
            ComponentSelector selector)
        {
            return (byte)(selector(endPoint1)
                          + (selector(endPoint2) - selector(endPoint1)) * lambda);
        }
    }
}