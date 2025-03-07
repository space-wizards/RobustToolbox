using Robust.Shared.Physics.Collision.Shapes;

namespace Robust.Shared.Physics.Collision;

internal interface IManifoldManager
{
    // TODO: this SUCKS but it's better than an edge for every contact.
    /// <summary>
    /// Gets an edge from objectpool.
    /// </summary>
    EdgeShape GetContactEdge();

    void ReturnEdge(EdgeShape edge);

    bool TestOverlap<T, U>(T shapeA, int indexA, U shapeB, int indexB, in Transform xfA, in Transform xfB)
        where T : IPhysShape
        where U : IPhysShape;

    void CollideCircles(ref Manifold manifold, PhysShapeCircle circleA, in Transform xfA,
        PhysShapeCircle circleB, in Transform xfB);

    void CollideEdgeAndCircle(ref Manifold manifold, EdgeShape edgeA, in Transform transformA,
        PhysShapeCircle circleB, in Transform transformB);

    void CollideEdgeAndPolygon(ref Manifold manifold, EdgeShape edgeA, in Transform xfA,
        PolygonShape polygonB, in Transform xfB);

    void CollidePolygonAndCircle(ref Manifold manifold, PolygonShape polygonA, in Transform xfA,
        PhysShapeCircle circleB, in Transform xfB);

    void CollidePolygons(ref Manifold manifold, PolygonShape polyA, in Transform transformA,
        PolygonShape polyB, in Transform transformB);
}
