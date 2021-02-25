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
using System.Runtime.InteropServices;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics.Collision
{
    public enum ManifoldType : byte
    {
        Invalid = 0,
        Circles,
        FaceA,
        FaceB,
    }

    internal enum ContactFeatureType : byte
    {
        Vertex = 0,
        Face = 1,
    }

    /// <summary>
    /// The features that intersect to form the contact point
    /// This must be 4 bytes or less.
    /// </summary>
    public struct ContactFeature
    {
        /// <summary>
        /// Feature index on ShapeA
        /// </summary>
        public byte IndexA;

        /// <summary>
        /// Feature index on ShapeB
        /// </summary>
        public byte IndexB;

        /// <summary>
        /// The feature type on ShapeA
        /// </summary>
        public byte TypeA;

        /// <summary>
        /// The feature type on ShapeB
        /// </summary>
        public byte TypeB;
    }

    /// <summary>
    /// Contact ids to facilitate warm starting.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct ContactID
    {
        /// <summary>
        /// The features that intersect to form the contact point
        /// </summary>
        [FieldOffset(0)]
        public ContactFeature Features;

        /// <summary>
        /// Used to quickly compare contact ids.
        /// </summary>
        [FieldOffset(0)]
        public uint Key;

        public static bool operator ==(ContactID id, ContactID other)
        {
            return id.Key == other.Key;
        }

        public static bool operator !=(ContactID id, ContactID other)
        {
            return !(id == other);
        }
    }

    public struct Manifold
    {
        public Vector2 LocalNormal;

        /// <summary>
        ///     Usage depends on manifold type.
        /// </summary>
        public Vector2 LocalPoint;

        public int PointCount;

        /// <summary>
        ///     Points of contact, can only be 0 -> 2.
        /// </summary>
        public ManifoldPoint[] Points;

        public ManifoldType Type;

        public Manifold(Vector2 localNormal, Vector2 localPoint, int pointCount, ManifoldPoint[] points, ManifoldType type)
        {
            LocalNormal = localNormal;
            LocalPoint = localPoint;
            PointCount = pointCount;
            Points = points;
            Type = type;
        }
    }

    public struct ManifoldPoint
    {
        /// <summary>
        ///     Unique identifier for the contact point between 2 shapes.
        /// </summary>
        public ContactID Id;

        /// <summary>
        ///     Usage depends on manifold type.
        /// </summary>
        public Vector2 LocalPoint;

        /// <summary>
        ///     The non-penetration impulse.
        /// </summary>
        public float NormalImpulse;

        /// <summary>
        ///     Friction impulse.
        /// </summary>
        public float TangentImpulse;

        public static bool operator ==(ManifoldPoint point, ManifoldPoint other)
        {
            return point.Id == other.Id &&
                   point.LocalPoint.Equals(other.LocalPoint) &&
                   point.NormalImpulse.Equals(other.NormalImpulse) &&
                   point.TangentImpulse.Equals(other.TangentImpulse);
        }

        public static bool operator !=(ManifoldPoint point, ManifoldPoint other)
        {
            return !(point == other);
        }
    }
}
