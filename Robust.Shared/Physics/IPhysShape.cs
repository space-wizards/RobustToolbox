using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    public interface IPhysShape : IExposeData
    {
        Box2 CalculateLocalBounds(Angle rotation);
    }
}
