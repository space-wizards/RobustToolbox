/*
Microsoft Permissive License (Ms-PL)

This license governs use of the accompanying software. If you use the software, you accept this license.
If you do not accept the license, do not use the software.

1. Definitions
The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under
U.S. copyright law.
A "contribution" is the original software, or any additions or changes to the software.
A "contributor" is any person that distributes its contribution under this license.
"Licensed patents" are a contributor's patent claims that read directly on its contribution.

2. Grant of Rights
(A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3,
each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution,
prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
(B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3,
each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to
make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or
derivative works of the contribution in the software.

3. Conditions and Limitations
(A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
(B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software,
your patent license from such contributor to the software ends automatically.
(C) If you distribute any portion of the software, you must retain all copyright, patent, trademark,
and attribution notices that are present in the software.
(D) If you distribute any portion of the software in source code form, you may do so only under this license by
including a complete copy of this license with your distribution.
If you distribute any portion of the software in compiled or object code form, you may only do so under a license that
complies with this license.
(E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees or conditions.
You may have additional consumer rights under your local laws which this license cannot change.
To the extent permitted under your local laws, the contributors exclude the implied warranties of
merchantability, fitness for a particular purpose and non-infringement.
*/

using System;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics.Collision
{

    internal static class DistanceManager
    {
        private const byte MaxGJKIterations = 20;

        public static void ComputeDistance(out DistanceOutput output, out SimplexCache cache, DistanceInput input)
        {
            cache = new SimplexCache();

            /*
            if (Settings.EnableDiagnostics) //FPE: We only gather diagnostics when enabled
                ++GJKCalls;
            */

            // Initialize the simplex.
            Simplex simplex = new Simplex();
            simplex.ReadCache(ref cache, input.ProxyA, ref input.TransformA, input.ProxyB, ref input.TransformB);

            // These store the vertices of the last simplex so that we
            // can check for duplicates and prevent cycling.
            var saveA = new int[3];
            var saveB = new int[3];

            //float distanceSqr1 = Settings.MaxFloat;

            // Main iteration loop.
            int iter = 0;
            while (iter < MaxGJKIterations)
            {
                // Copy simplex so we can identify duplicates.
                int saveCount = simplex.Count;
                for (var i = 0; i < saveCount; ++i)
                {
                    saveA[i] = simplex.V[i].IndexA;
                    saveB[i] = simplex.V[i].IndexB;
                }

                switch (simplex.Count)
                {
                    case 1:
                        break;
                    case 2:
                        simplex.Solve2();
                        break;
                    case 3:
                        simplex.Solve3();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                // If we have 3 points, then the origin is in the corresponding triangle.
                if (simplex.Count == 3)
                {
                    break;
                }

                //FPE: This code was not used anyway.
                // Compute closest point.
                //Vector2 p = simplex.GetClosestPoint();
                //float distanceSqr2 = p.LengthSquared();

                // Ensure progress
                //if (distanceSqr2 >= distanceSqr1)
                //{
                //break;
                //}
                //distanceSqr1 = distanceSqr2;

                // Get search direction.
                Vector2 d = simplex.GetSearchDirection();

                // Ensure the search direction is numerically fit.
                if (d.LengthSquared < float.Epsilon * float.Epsilon)
                {
                    // The origin is probably contained by a line segment
                    // or triangle. Thus the shapes are overlapped.

                    // We can't return zero here even though there may be overlap.
                    // In case the simplex is a point, segment, or triangle it is difficult
                    // to determine if the origin is contained in the CSO or very close to it.
                    break;
                }

                // Compute a tentative new simplex vertex using support points.
                SimplexVertex vertex = simplex.V[simplex.Count];
                vertex.IndexA = input.ProxyA.GetSupport(Transform.MulT(input.TransformA.Quaternion2D, -d));
                vertex.WA = Transform.Mul(input.TransformA, input.ProxyA.Vertices[vertex.IndexA]);

                vertex.IndexB = input.ProxyB.GetSupport(Transform.MulT(input.TransformB.Quaternion2D, d));
                vertex.WB = Transform.Mul(input.TransformB, input.ProxyB.Vertices[vertex.IndexB]);
                vertex.W = vertex.WB - vertex.WA;
                simplex.V[simplex.Count] = vertex;

                // Iteration count is equated to the number of support point calls.
                ++iter;

                /*
                if (Settings.EnableDiagnostics) //FPE: We only gather diagnostics when enabled
                    ++GJKIters;
                */

                // Check for duplicate support points. This is the main termination criteria.
                bool duplicate = false;
                for (int i = 0; i < saveCount; ++i)
                {
                    if (vertex.IndexA == saveA[i] && vertex.IndexB == saveB[i])
                    {
                        duplicate = true;
                        break;
                    }
                }

                // If we found a duplicate support point we must exit to avoid cycling.
                if (duplicate)
                {
                    break;
                }

                // New vertex is ok and needed.
                ++simplex.Count;
            }

            // Prepare output.
            simplex.GetWitnessPoints(out output.PointA, out output.PointB);
            output.Distance = (output.PointA - output.PointB).Length;
            output.Iterations = iter;

            // Cache the simplex.
            simplex.WriteCache(ref cache);

            // Apply radii if requested.
            if (input.UseRadii)
            {
                float rA = input.ProxyA.Radius;
                float rB = input.ProxyB.Radius;

                if (output.Distance > rA + rB && output.Distance > float.Epsilon)
                {
                    // Shapes are still no overlapped.
                    // Move the witness points to the outer surface.
                    output.Distance -= rA + rB;
                    Vector2 normal = output.PointB - output.PointA;
                    normal = normal.Normalized;
                    output.PointA += normal * rA;
                    output.PointB -= normal * rB;
                }
                else
                {
                    // Shapes are overlapped when radii are considered.
                    // Move the witness points to the middle.
                    Vector2 p = (output.PointA + output.PointB) * 0.5f;
                    output.PointA = p;
                    output.PointB = p;
                    output.Distance = 0.0f;
                }
            }
        }
    }
}
