/*
* Farseer Physics Engine:
* Copyright (c) 2012 Ian Qvist
*
* Original source Box2D:
* Copyright (c) 2006-2011 Erin Catto http://www.box2d.org
*
* This software is provided 'as-is', without any express or implied
* warranty.  In no event will the authors be held liable for any damages
* arising from the use of this software.
* Permission is granted to anyone to use this software for any purpose,
* including commercial applications, and to alter it and redistribute it
* freely, subject to the following restrictions:
* 1. The origin of this software must not be misrepresented; you must not
* claim that you wrote the original software. If you use this software
* in a product, an acknowledgment in the product documentation would be
* appreciated but is not required.
* 2. Altered source versions must be plainly marked as such, and must not be
* misrepresented as being the original software.
* 3. This notice may not be removed or altered from any source distribution.
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

    // Made a class because A) It gets mutated bloody everywhere and B) unless you're careful you'll get solver issues (yay!)
    // which I really could not be fucked dealing with
    public sealed class Manifold
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
        public ManifoldPoint[] Points = new ManifoldPoint[2];

        public ManifoldType Type;

        public Manifold() {}

        public Manifold(Vector2 localNormal, Vector2 localPoint, int pointCount, ManifoldPoint[] points, ManifoldType type)
        {
            LocalNormal = localNormal;
            LocalPoint = localPoint;
            PointCount = pointCount;
            // Do not remove this copy or shit BREAKS
            Array.Copy(points, Points, PointCount);
            Type = type;
        }

        internal Manifold Clone()
        {
            return new(LocalNormal, LocalPoint, PointCount, Points, Type);
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
