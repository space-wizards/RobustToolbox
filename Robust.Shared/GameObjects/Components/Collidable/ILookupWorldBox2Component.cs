using Robust.Shared.Maths;

namespace Robust.Shared.GameObjects
{
    public interface ILookupWorldBox2Component
    {
        Box2 GetWorldAABB(Vector2? worldPos = null, Angle? worldRot = null);
    }
}
