using Robust.Shared.Maths;

namespace Robust.Shared.Interfaces.Serialization.SharedDeepCloneExtensions
{
    [DeepCloneExtension(typeof(Color))]
    public class ColorDeepCloneExtension : DeepCloneExtension
    {
        public override object DeepClone(object value)
        {
            var color = (Color) value;
            return new Color(color.R, color.G, color.B, color.A);
        }
    }
}
