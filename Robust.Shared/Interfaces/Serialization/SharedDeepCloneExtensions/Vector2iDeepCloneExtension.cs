using Robust.Shared.Maths;

namespace Robust.Shared.Interfaces.Serialization.SharedDeepCloneExtensions
{
    [DeepCloneExtension(typeof(Vector2i))]
    public class Vector2iDeepCloneExtension : DeepCloneExtension
    {
        public override object DeepClone(object value)
        {
            var vec2 = (Vector2i) value;
            return new Vector2i
            {
                X = vec2.X,
                Y = vec2.Y
            };
        }
    }
}
