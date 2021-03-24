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
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Physics.Collision.Shapes
{
    /// <summary>
    ///     This holds the mass data computed for a shape.
    /// </summary>
    [DataDefinition]
    public struct MassData : IEquatable<MassData>
    {
        // Dotnet ports have Area calculated but box2d doesn't sooooo

        /// <summary>
        /// The position of the shape's centroid relative to the shape's origin.
        /// </summary>
        public Vector2 Centroid { get; internal set; }

        /// <summary>
        /// The rotational inertia of the shape about the local origin.
        /// </summary>
        public float Inertia { get; internal set; }

        /// <summary>
        /// The mass of the shape, usually in kilograms.
        /// </summary>
        [DataField("mass")]
        public float Mass { get; internal set; }

        /// <summary>
        /// The equal operator
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(MassData left, MassData right)
        {
            return (left.Mass == right.Mass && left.Centroid == right.Centroid && left.Inertia == right.Inertia);
        }

        /// <summary>
        /// The not equal operator
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(MassData left, MassData right)
        {
            return !(left == right);
        }

        public bool Equals(MassData other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            if (obj.GetType() != typeof(MassData))
                return false;

            return Equals((MassData)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = 0;
                result = (result * 397) ^ Centroid.GetHashCode();
                result = (result * 397) ^ Inertia.GetHashCode();
                result = (result * 397) ^ Mass.GetHashCode();
                return result;
            }
        }
    }
}
