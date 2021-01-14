using Robust.Shared.Maths;

namespace Robust.Shared.Interfaces.Serialization.SharedDeepCloneExtensions
{
    [DeepCloneExtension(typeof(UIBox2))]
    public class UIBox2DeepCloneExtension : DeepCloneExtension
    {
        public override object DeepClone(object value)
        {
            var box = (UIBox2) value;
            return new UIBox2(box.Left, box.Top, box.Right, box.Bottom);
        }
    }
}
