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
    public bool TestOverlap(IPhysShape shapeA, int childIndexA, IPhysShape shapeB, int childIndexB, in Transform xfA, in Transform xfB)
    {
        var input = new DistanceInput();

        input.ProxyA.Set(shapeA, childIndexA);
        input.ProxyB.Set(shapeB, childIndexB);
        input.TransformA = xfA;
        input.TransformB = xfB;
        input.UseRadii = true;

        DistanceManager.ComputeDistance(out var output, out _, input);

        return output.Distance < 10.0f * float.Epsilon;
    }
}
