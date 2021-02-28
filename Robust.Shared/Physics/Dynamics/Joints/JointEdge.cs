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

using Robust.Shared.GameObjects;

namespace Robust.Shared.Physics.Dynamics.Joints
{
    /// <summary>
    /// A joint edge is used to connect bodies and joints together
    /// in a joint graph where each body is a node and each joint
    /// is an edge. A joint edge belongs to a doubly linked list
    /// maintained in each attached body. Each joint has two joint
    /// nodes, one for each attached body.
    /// </summary>
    public sealed class JointEdge
    {
        /// <summary>
        /// The joint.
        /// </summary>
        public Joint Joint { get; set; } = default!;

        /// <summary>
        /// The next joint edge in the body's joint list.
        /// </summary>
        public JointEdge? Next { get; set; }

        /// <summary>
        /// Provides quick access to the other body attached.
        /// </summary>
        public PhysicsComponent Other { get; set; } = default!;

        /// <summary>
        /// The previous joint edge in the body's joint list.
        /// </summary>
       public JointEdge? Prev { get; set; }
    }
}
