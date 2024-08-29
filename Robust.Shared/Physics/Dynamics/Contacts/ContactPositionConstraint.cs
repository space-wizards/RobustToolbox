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

using System.Numerics;
using Robust.Shared.Physics.Collision;

namespace Robust.Shared.Physics.Dynamics.Contacts
{
    internal struct ContactPositionConstraint
    {
        /// <summary>
        ///     Index of BodyA in the island.
        /// </summary>
        public int IndexA { get; set; }

        /// <summary>
        ///     Index of BodyB in the island.
        /// </summary>
        public int IndexB { get; set; }

        public Vector2[] LocalPoints;

        public Vector2 LocalNormal;

        public Vector2 LocalPoint;

        public float InvMassA;

        public float InvMassB;

        public Vector2 LocalCenterA;

        public Vector2 LocalCenterB;

        public float InvIA;

        public float InvIB;

        public ManifoldType Type;

        public float RadiusA;

        public float RadiusB;

        public int PointCount;
    }
}
