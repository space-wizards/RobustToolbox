using Robust.Shared.Physics.Collision.Shapes;

namespace Robust.Shared.Physics.Collision;

internal sealed partial class CollisionManager
{
    /// <summary>
    /// Test overlap between the two shapes.
    /// </summary>
    /// <param name="shapeA">The first shape.</param>
    /// <param name="shapeB">The second shape.</param>
    /// <param name="xfA">The transform for the first shape.</param>
    /// <param name="xfB">The transform for the seconds shape.</param>
    /// <returns></returns>
    public bool TestOverlap<T, U>(
        T shapeA,
        int indexA,
        U shapeB,
        int indexB,
        in Transform xfA,
        in Transform xfB,
        bool ignoreShapeSkin = false)
        where T : IPhysShape
        where U : IPhysShape
    {
        var input = new DistanceInput();

        input.ProxyA.Set(in shapeA, indexA);
        input.ProxyB.Set(in shapeB, indexB);

        if (ignoreShapeSkin)
        {
            if (shapeA.ShapeType != ShapeType.Circle)
                input.ProxyA.Radius = 0f;

            if (shapeB.ShapeType != ShapeType.Circle)
                input.ProxyB.Radius = 0f;
        }

        input.TransformA = xfA;
        input.TransformB = xfB;
        input.UseRadii = true;

        DistanceManager.ComputeDistance(out var output, out _, input);

        return output.Distance < 10.0f * float.Epsilon;
    }
}
