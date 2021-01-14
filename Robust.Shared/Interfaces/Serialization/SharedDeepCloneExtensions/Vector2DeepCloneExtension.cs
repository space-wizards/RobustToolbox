using System.Numerics;

namespace Robust.Shared.Interfaces.Serialization.SharedDeepCloneExtensions
{
    [DeepCloneExtension(typeof(Vector2))]
    public class Vector2DeepCloneExtension : DeepCloneExtension
    {
        public override object DeepClone(object value)
        {
            var vec2 = (Vector2) value;
            return new Vector2
            {
                X = vec2.X,
                Y = vec2.Y
            };
        }
    }
}
