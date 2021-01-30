using Robust.Shared.Maths;

namespace Robust.Shared.Interfaces.Serialization.SharedDeepCloneExtensions
{
    [DeepCloneExtension(typeof(Angle))]
    public class AngleDeepCloneExtension : DeepCloneExtension
    {
        public override object DeepClone(object value)
        {
            var angle = (Angle) value;
            return new Angle(angle);
        }
    }
}
