using System;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Shapes;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Systems;

public sealed partial class RayCastSystem
{
    /*
     * This is really "geometry and friends" as it has all the private methods.
     */

    #region Callbacks

    /// <summary>
    /// Returns every entity from the callback.
    /// </summary>
    public static float RayCastAllCallback(FixtureProxy proxy, Vector2 point, Vector2 normal, float fraction, ref RayResult result)
    {
        result.Results.Add(new RayHit(proxy.Entity, normal, fraction)
        {
            Point = point,
        });
        return 1f;
    }

    /// <summary>
    /// Gets the closest entity from the callback.
    /// </summary>
    public static float RayCastClosestCallback(FixtureProxy proxy, Vector2 point, Vector2 normal, float fraction, ref RayResult result)
    {
        var add = false;

        if (result.Results.Count > 0)
        {
            if (result.Results[0].Fraction > fraction)
            {
                add = true;
                result.Results.Clear();
            }
        }
        else
        {
            add = true;
        }

        if (add)
        {
            result.Results.Add(new RayHit(proxy.Entity, normal, fraction)
            {
                Point = point,
            });
        }

        return fraction;
    }

    #endregion

    #region Raycast

    private CastOutput RayCastShape(RayCastInput input, IPhysShape shape, Transform transform)
    {
        var localInput = input;
        localInput.Origin = Physics.Transform.InvTransformPoint(transform, input.Origin);
        localInput.Translation = Quaternion2D.InvRotateVector(transform.Quaternion2D, input.Translation);

        CastOutput output = new();

        switch (shape)
        {
            /*
            case b2_capsuleShape:
                output = b2RayCastCapsule( &localInput, &shape->capsule );
                break;
                */
            case PhysShapeCircle circle:
                output = RayCastCircle(localInput, circle);
                break;
            case PolygonShape polyShape:
            {
                output = RayCastPolygon(localInput, (Polygon) polyShape);
            }
                break;
            case Polygon poly:
            {
                output = RayCastPolygon(localInput, poly);
            }
                break;
            default:
                return output;
        }

        output.Point = Physics.Transform.Mul(transform, output.Point);
        output.Normal = Quaternion2D.RotateVector(transform.Quaternion2D, output.Normal);
        return output;
    }

    /// <summary>
    /// This callback is invoked upon every AABB collision.
    /// </summary>
    private static float RayCastCallback(RayCastInput input, FixtureProxy proxy, ref WorldRayCastContext worldContext)
    {
        if ((proxy.Fixture.CollisionLayer & worldContext.Filter.MaskBits) == 0 && (proxy.Fixture.CollisionMask & worldContext.Filter.LayerBits) == 0)
        {
            return input.MaxFraction;
        }

        if (worldContext.Filter.IsIgnored?.Invoke(proxy.Entity) == true)
        {
            return input.MaxFraction;
        }

        var transform = worldContext.Physics.GetLocalPhysicsTransform(proxy.Entity);
        var output = worldContext.System.RayCastShape(input, proxy.Fixture.Shape, transform);

        if (output.Hit)
        {
            // Fraction returned determines what B2Dynamictree will do, i.e. shrink the AABB or not.
            var fraction = worldContext.fcn(proxy, output.Point, output.Normal, output.Fraction, ref worldContext.Result);
            return fraction;
        }

        return input.MaxFraction;
    }

    // Precision Improvements for Ray / Sphere Intersection - Ray Tracing Gems 2019
    // http://www.codercorner.com/blog/?p=321
    internal CastOutput RayCastCircle(RayCastInput input, PhysShapeCircle shape)
    {
        DebugTools.Assert(input.IsValidRay());

        var p = shape.Position;

        var output = new CastOutput();

        // Shift ray so circle center is the origin
        var s = Vector2.Subtract(input.Origin, p);
        float length = 0f;
        var d = input.Translation.GetLengthAndNormalize(ref length);
        if (length == 0.0f)
        {
            // zero length ray
            return output;
        }

        // Find closest point on ray to origin

        // solve: dot(s + t * d, d) = 0
        float t = -Vector2.Dot(s, d);

        // c is the closest point on the line to the origin
        var c = Vector2.Add(s, t * d);

        float cc = Vector2.Dot(c, c);
        float r = shape.Radius;
        float rr = r * r;

        if (cc > rr)
        {
            // closest point is outside the circle
            return output;
        }

        // Pythagorus
        float h = MathF.Sqrt(rr - cc);

        float fraction = t - h;

        if ( fraction < 0.0f || input.MaxFraction * length < fraction )
        {
            // outside the range of the ray segment
            return output;
        }

        var hitPoint = Vector2.Add(s, fraction * d);

        output.Fraction = fraction / length;
        output.Normal = hitPoint.Normalized();
        output.Point = Vector2.Add(p, shape.Radius * output.Normal);
        output.Hit = true;

        return output;
    }

    private CastOutput RayCastPolygon(RayCastInput input, Polygon shape)
    {
        var verts = shape._vertices.AsSpan;
        var output = new CastOutput()
        {
            Fraction = 0f,
        };

	    if (shape.Radius == 0.0f)
	    {
		    // Put the ray into the polygon's frame of reference.
		    var p1 = input.Origin;
		    var d = input.Translation;

		    float lower = 0.0f, upper = input.MaxFraction;

		    var index = -1;

            var norms = shape._normals.AsSpan;

		    for ( var i = 0; i < shape.VertexCount; ++i )
		    {
			    // p = p1 + a * d
			    // dot(normal, p - v) = 0
			    // dot(normal, p1 - v) + a * dot(normal, d) = 0
			    float numerator = Vector2.Dot(norms[i], Vector2.Subtract( verts[i], p1 ) );
			    float denominator = Vector2.Dot(norms[i], d );

			    if ( denominator == 0.0f )
			    {
				    if ( numerator < 0.0f )
				    {
					    return output;
				    }
			    }
			    else
			    {
				    // Note: we want this predicate without division:
				    // lower < numerator / denominator, where denominator < 0
				    // Since denominator < 0, we have to flip the inequality:
				    // lower < numerator / denominator <==> denominator * lower > numerator.
				    if ( denominator < 0.0f && numerator < lower * denominator )
				    {
					    // Increase lower.
					    // The segment enters this half-space.
					    lower = numerator / denominator;
					    index = i;
				    }
				    else if ( denominator > 0.0f && numerator < upper * denominator )
				    {
					    // Decrease upper.
					    // The segment exits this half-space.
					    upper = numerator / denominator;
				    }
			    }

			    // The use of epsilon here causes the B2_ASSERT on lower to trip
			    // in some cases. Apparently the use of epsilon was to make edge
			    // shapes work, but now those are handled separately.
			    // if (upper < lower - b2_epsilon)
			    if ( upper < lower )
			    {
				    return output;
			    }
		    }

		    DebugTools.Assert( 0.0f <= lower && lower <= input.MaxFraction );

		    if (index >= 0)
		    {
			    output.Fraction = lower;
			    output.Normal = norms[index];
			    output.Point = Vector2.Add(p1, lower * d);
			    output.Hit = true;
		    }

		    return output;
	    }

        Span<Vector2> proxyBVerts = new Vector2[]
        {
            input.Origin,
        };

        // TODO_ERIN this is not working for ray vs box (zero radii)
	    var castInput = new ShapeCastPairInput
        {
            ProxyA = DistanceProxy.MakeProxy(verts, shape.VertexCount, shape.Radius),
            ProxyB = DistanceProxy.MakeProxy(proxyBVerts, 1, 0.0f),
            TransformA = Physics.Transform.Empty,
            TransformB = Physics.Transform.Empty,
            TranslationB = input.Translation,
            MaxFraction = input.MaxFraction
        };

        ShapeCast(ref output, castInput);
        return output;
    }

    // Ray vs line segment
    private CastOutput RayCastSegment(RayCastInput input, EdgeShape shape, bool oneSided)
    {
        var output = new CastOutput();

        if (oneSided)
        {
            // Skip left-side collision
            float offset = Vector2Helpers.Cross(Vector2.Subtract(input.Origin, shape.Vertex0), Vector2.Subtract( shape.Vertex1, shape.Vertex0));
            if ( offset < 0.0f )
            {
                return output;
            }
        }

        // Put the ray into the edge's frame of reference.
        var p1 = input.Origin;
        var d = input.Translation;

        var v1 = shape.Vertex0;
        var v2 = shape.Vertex1;
        var e = Vector2.Subtract( v2, v1 );

        float length = 0f;
        var eUnit = e.GetLengthAndNormalize(ref length);
        if (length == 0.0f)
        {
            return output;
        }

        // Normal points to the right, looking from v1 towards v2
        var normal = eUnit.RightPerp();

        // Intersect ray with infinite segment using normal
        // Similar to intersecting a ray with an infinite plane
        // p = p1 + t * d
        // dot(normal, p - v1) = 0
        // dot(normal, p1 - v1) + t * dot(normal, d) = 0
        float numerator = Vector2.Dot(normal, Vector2.Subtract(v1, p1));
        float denominator = Vector2.Dot(normal, d);

        if (denominator == 0.0f)
        {
            // parallel
            return output;
        }

        float t = numerator / denominator;
        if ( t < 0.0f || input.MaxFraction < t )
        {
            // out of ray range
            return output;
        }

        // Intersection point on infinite segment
        var p = Vector2.Add(p1, t * d);

        // Compute position of p along segment
        // p = v1 + s * e
        // s = dot(p - v1, e) / dot(e, e)

        float s = Vector2.Dot(Vector2.Subtract(p, v1), eUnit);
        if ( s < 0.0f || length < s )
        {
            // out of segment range
            return output;
        }

        if ( numerator > 0.0f )
        {
            normal = -normal;
        }

        output.Fraction = t;
        output.Point = Vector2.Add(p1, t * d);
        output.Normal = normal;
        output.Hit = true;

        return output;
    }

    #endregion

    #region Shape

    private CastOutput ShapeCastShape(ShapeCastInput input, IPhysShape shape, Transform transform)
    {
        var localInput = input;

        for ( int i = 0; i < localInput.Count; ++i )
        {
            localInput.Points[i] = Physics.Transform.MulT(transform, input.Points[i]);
        }

        localInput.Translation = Quaternion2D.InvRotateVector(transform.Quaternion2D, input.Translation);

        CastOutput output;

        switch (shape)
        {
            case PhysShapeCircle circle:
                output = ShapeCastCircle(localInput, circle);
                break;
            case PolygonShape pShape:
                output = ShapeCastPolygon(localInput, (Polygon) pShape);
                break;
            case Polygon poly:
                output = ShapeCastPolygon(localInput, poly);
                break;
            default:
                return new CastOutput();
        }

        output.Point = Physics.Transform.Mul(transform, output.Point);
        output.Normal = Quaternion2D.RotateVector(transform.Quaternion2D, output.Normal);
        return output;
    }

    /// <summary>
    /// This callback is invoked upon getting the AABB inside of B2DynamicTree.
    /// </summary>
    /// <returns>The max fraction to continue checking for. If this is lower then we will start dropping more shapes early</returns>
    private float ShapeCastCallback(ShapeCastInput input, FixtureProxy proxy, ref WorldRayCastContext worldContext)
    {
        var filter = worldContext.Filter;

        if ((proxy.Fixture.CollisionLayer & filter.MaskBits) == 0 && (proxy.Fixture.CollisionMask & filter.LayerBits) == 0)
        {
            return input.MaxFraction;
        }

        if ((filter.Flags & QueryFlags.Sensors) == 0x0 && !proxy.Fixture.Hard)
        {
            return input.MaxFraction;
        }

        if (worldContext.Filter.IsIgnored?.Invoke(proxy.Entity) == true)
        {
            return input.MaxFraction;
        }

        var transform = worldContext.Physics.GetLocalPhysicsTransform(proxy.Entity);
        var output = ShapeCastShape(input, proxy.Fixture.Shape, transform);

        if (output.Hit)
        {
            var fraction = worldContext.fcn(proxy, output.Point, output.Normal, output.Fraction, ref worldContext.Result);
            return fraction;
        }

        return input.MaxFraction;
    }

    // GJK-raycast
    // Algorithm by Gino van den Bergen.
    // "Smooth Mesh Contacts with GJK" in Game Physics Pearls. 2010
    // todo this is failing when used to raycast a box
    // todo this converges slowly with a radius
    private void ShapeCast(ref CastOutput output, in ShapeCastPairInput input)
    {
        output.Fraction = input.MaxFraction;

	    var proxyA = input.ProxyA;
        var count = input.ProxyB.Vertices.Length;

	    var xfA = input.TransformA;
	    var xfB = input.TransformB;
	    var xf = Physics.Transform.InvMulTransforms(xfA, xfB);

	    // Put proxyB in proxyA's frame to reduce round-off error
        var proxyBVerts = new Vector2[input.ProxyB.Vertices.Length];

	    for ( int i = 0; i < count; ++i )
	    {
		    proxyBVerts[i] = Physics.Transform.Mul(xf, input.ProxyB.Vertices[i]);
	    }

        var proxyB = DistanceProxy.MakeProxy(proxyBVerts, count, input.ProxyB.Radius);

        DebugTools.Assert(proxyB.Vertices.Length <= PhysicsConstants.MaxPolygonVertices);
	    float radius = proxyA.Radius + proxyB.Radius;

	    var r = Quaternion2D.RotateVector(xf.Quaternion2D, input.TranslationB);
	    float lambda = 0.0f;
	    float maxFraction = input.MaxFraction;

	    // Initial simplex
	    Simplex simplex;
        simplex = new()
        {
            Count = 0,
            V = new FixedArray4<SimplexVertex>()
        };

	    // Get an initial point in A - B
	    int indexA = FindSupport(proxyA, -r);
	    var wA = proxyA.Vertices[indexA];
	    int indexB = FindSupport(proxyB, r);
	    var wB = proxyB.Vertices[indexB];
	    var v = Vector2.Subtract(wA, wB);

	    // Sigma is the target distance between proxies
	    const float linearSlop = PhysicsConstants.LinearSlop;
	    var sigma = MathF.Max(linearSlop, radius - linearSlop);

	    // Main iteration loop.
	    const int k_maxIters = 20;
	    int iter = 0;
	    while ( iter < k_maxIters && v.Length() > sigma + 0.5f * linearSlop )
	    {
		    DebugTools.Assert(simplex.Count < 3);

		    output.Iterations += 1;

		    // Support in direction -v (A - B)
		    indexA = FindSupport(proxyA, -v);
		    wA = proxyA.Vertices[indexA];
		    indexB = FindSupport(proxyB, v);
		    wB = proxyB.Vertices[indexB];
		    var p = Vector2.Subtract(wA, wB);

		    // -v is a normal at p, normalize to work with sigma
		    v = v.Normalized();

		    // Intersect ray with plane
		    float vp = Vector2.Dot(v, p);
		    float vr = Vector2.Dot(v, r);
		    if ( vp - sigma > lambda * vr )
		    {
			    if ( vr <= 0.0f )
			    {
				    // miss
                    return;
                }

			    lambda = ( vp - sigma ) / vr;
			    if ( lambda > maxFraction )
			    {
				    // too far
                    return;
                }

			    // reset the simplex
			    simplex.Count = 0;
		    }

		    // Reverse simplex since it works with B - A.
		    // Shift by lambda * r because we want the closest point to the current clip point.
		    // Note that the support point p is not shifted because we want the plane equation
		    // to be formed in unshifted space.
		    ref var vertex = ref simplex.V.AsSpan[simplex.Count];
		    vertex.IndexA = indexB;
		    vertex.WA = new Vector2(wB.X + lambda * r.X, wB.Y + lambda * r.Y);
		    vertex.IndexB = indexA;
		    vertex.WB = wA;
		    vertex.W = Vector2.Subtract(vertex.WB, vertex.WA);
		    vertex.A = 1.0f;
		    simplex.Count += 1;

		    switch (simplex.Count)
		    {
			    case 1:
				    break;

			    case 2:
				    Simplex.SolveSimplex2(ref simplex);
				    break;

			    case 3:
				    Simplex.SolveSimplex3(ref simplex);
				    break;

			    default:
                    throw new NotImplementedException();
		    }

		    // If we have 3 points, then the origin is in the corresponding triangle.
		    if ( simplex.Count == 3 )
		    {
			    // Overlap
                // Yes this means you need to manually query for overlaps.
			    return;
		    }

		    // Get search direction.
		    // todo use more accurate segment perpendicular
		    v = Simplex.ComputeSimplexClosestPoint(simplex);

		    // Iteration count is equated to the number of support point calls.
		    ++iter;
	    }

	    if ( iter == 0 || lambda == 0.0f )
	    {
		    // Initial overlap
		    return;
	    }

	    // Prepare output.
	    Vector2 pointA = Vector2.Zero, pointB = Vector2.Zero;
	    Simplex.ComputeSimplexWitnessPoints(ref pointB, ref pointA, simplex);

	    var n = (-v).Normalized();
	    var point = new Vector2(pointA.X + proxyA.Radius * n.X, pointA.Y + proxyA.Radius * n.Y);

	    output.Point = Physics.Transform.Mul(xfA, point);
	    output.Normal = Quaternion2D.RotateVector(xfA.Quaternion2D, n);
	    output.Fraction = lambda;
	    output.Iterations = iter;
	    output.Hit = true;
    }

    private int FindSupport(DistanceProxy proxy, Vector2 direction)
    {
        int bestIndex = 0;
        float bestValue = Vector2.Dot(proxy.Vertices[0], direction);
        for ( int i = 1; i < proxy.Vertices.Length; ++i )
        {
            float value = Vector2.Dot(proxy.Vertices[i], direction);
            if ( value > bestValue )
            {
                bestIndex = i;
                bestValue = value;
            }
        }

        return bestIndex;
    }

    private CastOutput ShapeCastCircle(ShapeCastInput input, PhysShapeCircle shape)
    {
        Span<Vector2> proxyAVerts = new[]
        {
            shape.Position,
        };

        var pairInput = new ShapeCastPairInput
        {
            ProxyA = DistanceProxy.MakeProxy(proxyAVerts, 1, shape.Radius ),
            ProxyB = DistanceProxy.MakeProxy(input.Points, input.Count, input.Radius ),
            TransformA = Physics.Transform.Empty,
            TransformB = Physics.Transform.Empty,
            TranslationB = input.Translation,
            MaxFraction = input.MaxFraction
        };

        var output = new CastOutput();
        ShapeCast(ref output, pairInput);
        return output;
    }

    private CastOutput ShapeCastPolygon(ShapeCastInput input, Polygon shape)
    {
        var pairInput = new ShapeCastPairInput
        {
            ProxyA = DistanceProxy.MakeProxy(shape._vertices.AsSpan, shape.VertexCount, shape.Radius),
            ProxyB = DistanceProxy.MakeProxy(input.Points, input.Count, input.Radius),
            TransformA = Physics.Transform.Empty,
            TransformB = Physics.Transform.Empty,
            TranslationB = input.Translation,
            MaxFraction = input.MaxFraction
        };

        var output = new CastOutput();
        ShapeCast(ref output, pairInput);
        return output;
    }

    private CastOutput ShapeCastSegment(ShapeCastInput input, EdgeShape shape)
    {
        Span<Vector2> proxyAVerts = new[]
        {
            shape.Vertex0,
        };

        var pairInput = new ShapeCastPairInput
        {
            ProxyA = DistanceProxy.MakeProxy(proxyAVerts, 2, 0.0f),
            ProxyB = DistanceProxy.MakeProxy(input.Points, input.Count, input.Radius),
            TransformA = Physics.Transform.Empty,
            TransformB = Physics.Transform.Empty,
            TranslationB = input.Translation,
            MaxFraction = input.MaxFraction
        };

        var output = new CastOutput();
        ShapeCast(ref output, pairInput);
        return output;
    }

    #endregion
}

internal ref struct WorldRayCastContext
{
    public RayCastSystem System;
    public SharedPhysicsSystem Physics;

    public CastResult fcn;
    public QueryFilter Filter;
    public float Fraction;

    public RayResult Result;
}

internal ref struct ShapeCastPairInput
{
    public DistanceProxy ProxyA;
    public DistanceProxy ProxyB;
    public Transform TransformA;
    public Transform TransformB;
    public Vector2 TranslationB;

    /// <summary>
    /// The fraction of the translation to consider, typically 1
    /// </summary>
    public float MaxFraction;
}

internal record struct ShapeCastInput
{
    public Transform Origin;

    /// A point cloud to cast
    public Vector2[] Points;

    /// The number of points
    public int Count;

    /// The radius around the point cloud
    public float Radius;

    /// The translation of the shape cast
    public Vector2 Translation;

    /// The maximum fraction of the translation to consider, typically 1
    public float MaxFraction;
}

internal record struct RayCastInput
{
    public Vector2 Origin;

    public Vector2 Translation;

    public float MaxFraction;

    public bool IsValidRay()
    {
        bool isValid = Origin.IsValid() && Translation.IsValid() && MaxFraction.IsValid() &&
                       0.0f <= MaxFraction && MaxFraction < float.MaxValue;
        return isValid;
    }
}

internal ref struct CastOutput
{
    public Vector2 Normal;

    public Vector2 Point;

    public float Fraction;

    public int Iterations;

    public bool Hit;
}
