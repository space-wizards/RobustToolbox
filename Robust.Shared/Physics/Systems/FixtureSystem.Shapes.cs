using System;
using System.Numerics;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Shapes;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Systems
{
    public partial class FixtureSystem
    {
        /// <summary>
        /// Tests whether a particular point is contained in the shape.
        /// </summary>
        public bool TestPoint(IPhysShape shape, Transform xform, Vector2 worldPoint)
        {
            switch (shape)
            {
                case ChainShape:
                case EdgeShape:
                    return false;
                case PhysShapeAabb aabb:
                    // TODO: When we get actual AABBs it will be a stupid ez check,
                    var polygon = (PolygonShape) aabb;
                    return TestPoint(polygon, xform, worldPoint);
                case PhysShapeCircle circle:
                    var center = xform.Position + Physics.Transform.Mul(xform.Quaternion2D, circle.Position);
                    var distance = worldPoint - center;
                    return Vector2.Dot(distance, distance) <= circle.Radius * circle.Radius;
                case PolygonShape poly:
                {
                    var pLocal = Physics.Transform.MulT(xform.Quaternion2D, worldPoint - xform.Position);

                    for (var i = 0; i < poly.VertexCount; i++)
                    {
                        var dot = Vector2.Dot(poly.Normals[i], pLocal - poly.Vertices[i]);
                        if (dot > 0f) return false;
                    }

                    return true;
                }
                case Polygon poly:
                {
                    var pLocal = Physics.Transform.MulT(xform.Quaternion2D, worldPoint - xform.Position);

                    for (var i = 0; i < poly.VertexCount; i++)
                    {
                        var dot = Vector2.Dot(poly.Normals[i], pLocal - poly.Vertices[i]);
                        if (dot > 0f) return false;
                    }

                    return true;
                }
                default:
                    throw new ArgumentOutOfRangeException($"No implemented TestPoint for {shape.GetType()}");
            }
        }

        public static MassData GetMassData(IPhysShape shape, float density)
        {
            var data = new MassData();

            // Box2D just calls fixture.GetMassData which just calls the shape method anyway soooo
            // we can just cut out the middle-man
            switch (shape)
            {
                case ChainShape:
                    data.Mass = 0f;
                    data.Center = Vector2.Zero;
                    data.I = 0f;
                    break;
                case EdgeShape edge:
                    data.Mass = 0.0f;
                    data.Center = (edge.Vertex1 + edge.Vertex2) * 0.5f;
                    data.I = 0.0f;
                    break;
                case PhysShapeCircle circle:
                    // massData->mass = density * b2_pi * m_radius * m_radius;
                    data.Center = circle.Position;

                    // inertia about the local origin
                    data.I = data.Mass * (0.5f * circle.Radius * circle.Radius + Vector2.Dot(circle.Position, circle.Position));
                    break;
                case PhysShapeAabb aabb:
                    var polygon = (PolygonShape) aabb;
                    GetMassData(polygon, ref data, density);
                    break;
                case PolygonShape poly:
                    // Polygon mass, centroid, and inertia.
                    // Let rho be the polygon density in mass per unit area.
                    // Then:
                    // mass = rho * int(dA)
                    // centroid.x = (1/mass) * rho * int(x * dA)
                    // centroid.y = (1/mass) * rho * int(y * dA)
                    // I = rho * int((x*x + y*y) * dA)
                    //
                    // We can compute these integrals by summing all the integrals
                    // for each triangle of the polygon. To evaluate the integral
                    // for a single triangle, we make a change of variables to
                    // the (u,v) coordinates of the triangle:
                    // x = x0 + e1x * u + e2x * v
                    // y = y0 + e1y * u + e2y * v
                    // where 0 <= u && 0 <= v && u + v <= 1.
                    //
                    // We integrate u from [0,1-v] and then v from [0,1].
                    // We also need to use the Jacobian of the transformation:
                    // D = cross(e1, e2)
                    //
                    // Simplification: triangle centroid = (1/3) * (p1 + p2 + p3)
                    //
                    // The rest of the derivation is handled by computer algebra.

                    var count = poly.VertexCount;
                    DebugTools.Assert(count >= 3);

                    Vector2 center = new(0.0f, 0.0f);
                    var area = 0.0f;
                    var I = 0.0f;

                    // Get a reference point for forming triangles.
                    // Use the first vertex to reduce round-off errors.
                    var s = poly.Vertices[0];

                    const float k_inv3 = 1.0f / 3.0f;

                    for (var i = 0; i < count; ++i)
                    {
	                    // Triangle vertices.
	                    var e1 = poly.Vertices[i] - s;
	                    var e2 = i + 1 < count ? poly.Vertices[i+1] - s : poly.Vertices[0] - s;

	                    var D = Vector2Helpers.Cross(e1, e2);

	                    var triangleArea = 0.5f * D;
	                    area += triangleArea;

	                    // Area weighted centroid
	                    center += (e1 + e2) * triangleArea * k_inv3;

	                    float ex1 = e1.X, ey1 = e1.Y;
	                    float ex2 = e2.X, ey2 = e2.Y;

	                    var intx2 = ex1*ex1 + ex2*ex1 + ex2*ex2;
	                    var inty2 = ey1*ey1 + ey2*ey1 + ey2*ey2;

	                    I += (0.25f * k_inv3 * D) * (intx2 + inty2);
                    }

                    // Total mass
                    data.Mass = density * area;

                    // Center of mass
                    DebugTools.Assert(area > float.Epsilon);
                    center *= 1.0f / area;
                    data.Center = center + s;

                    // Inertia tensor relative to the local origin (point s).
                    data.I = density * I;

                    // Shift to center of mass then to original body origin.
                    data.I += data.Mass * (Vector2.Dot(data.Center, data.Center) - Vector2.Dot(center, center));
                    break;
                default:
                    throw new NotImplementedException($"Cannot get MassData for {shape} as it's not implemented!");
            }

            return data;
        }

        public static void GetMassData(IPhysShape shape, ref MassData data, float density)
        {
            // Box2D just calls fixture.GetMassData which just calls the shape method anyway soooo
            // we can just cut out the middle-man
            switch (shape)
            {
                case ChainShape:
                    data.Mass = 0f;
                    data.Center = Vector2.Zero;
                    data.I = 0f;
                    break;
                case EdgeShape edge:
                    data.Mass = 0.0f;
                    data.Center = (edge.Vertex1 + edge.Vertex2) * 0.5f;
                    data.I = 0.0f;
                    break;
                case PhysShapeCircle circle:
                    data.Mass = density * MathF.PI * circle.Radius * circle.Radius;
                    data.Center = circle.Position;

                    // inertia about the local origin
                    data.I = data.Mass * (0.5f * circle.Radius * circle.Radius + Vector2.Dot(circle.Position, circle.Position));
                    break;
                case PhysShapeAabb aabb:
                    var polygon = (PolygonShape) aabb;
                    GetMassData(polygon, ref data, density);
                    break;
                case PolygonShape poly:
                    // Polygon mass, centroid, and inertia.
                    // Let rho be the polygon density in mass per unit area.
                    // Then:
                    // mass = rho * int(dA)
                    // centroid.x = (1/mass) * rho * int(x * dA)
                    // centroid.y = (1/mass) * rho * int(y * dA)
                    // I = rho * int((x*x + y*y) * dA)
                    //
                    // We can compute these integrals by summing all the integrals
                    // for each triangle of the polygon. To evaluate the integral
                    // for a single triangle, we make a change of variables to
                    // the (u,v) coordinates of the triangle:
                    // x = x0 + e1x * u + e2x * v
                    // y = y0 + e1y * u + e2y * v
                    // where 0 <= u && 0 <= v && u + v <= 1.
                    //
                    // We integrate u from [0,1-v] and then v from [0,1].
                    // We also need to use the Jacobian of the transformation:
                    // D = cross(e1, e2)
                    //
                    // Simplification: triangle centroid = (1/3) * (p1 + p2 + p3)
                    //
                    // The rest of the derivation is handled by computer algebra.

                    var count = poly.VertexCount;
                    DebugTools.Assert(count >= 3);

                    Vector2 center = new(0.0f, 0.0f);
                    float area = 0.0f;
                    float I = 0.0f;

                    // Get a reference point for forming triangles.
                    // Use the first vertex to reduce round-off errors.
                    var s = poly.Vertices[0];

                    const float k_inv3 = 1.0f / 3.0f;

                    for (var i = 0; i < count; ++i)
                    {
	                    // Triangle vertices.
	                    var e1 = poly.Vertices[i] - s;
	                    var e2 = i + 1 < count ? poly.Vertices[i+1] - s : poly.Vertices[0] - s;

	                    float D = Vector2Helpers.Cross(e1, e2);

	                    float triangleArea = 0.5f * D;
	                    area += triangleArea;

	                    // Area weighted centroid
	                    center += (e1 + e2) * triangleArea * k_inv3;

	                    float ex1 = e1.X, ey1 = e1.Y;
	                    float ex2 = e2.X, ey2 = e2.Y;

	                    float intx2 = ex1*ex1 + ex2*ex1 + ex2*ex2;
	                    float inty2 = ey1*ey1 + ey2*ey1 + ey2*ey2;

	                    I += (0.25f * k_inv3 * D) * (intx2 + inty2);
                    }

                    // Total mass
                    data.Mass = density * area;

                    // Center of mass
                    DebugTools.Assert(area > float.Epsilon);
                    center *= 1.0f / area;
                    data.Center = center + s;

                    // Inertia tensor relative to the local origin (point s).
                    data.I = density * I;

                    // Shift to center of mass then to original body origin.
                    data.I += data.Mass * (Vector2.Dot(data.Center, data.Center) - Vector2.Dot(center, center));
                    break;
                default:
                    throw new NotImplementedException($"Cannot get MassData for {shape} as it's not implemented!");
            }
        }
    }
}
