using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.Shared.GameObjects
{
    public interface ILookupWorldBox2Component
    {
        Box2 GetAABB(Transform transform);

        Box2 GetWorldAABB(Vector2? worldPos = null, Angle? worldRot = null);
    }
}
