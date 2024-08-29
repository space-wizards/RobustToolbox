﻿using System;

namespace Robust.Shared.Physics
{
    public static class PhysicsConstants
    {
        /// <summary>
        /// The radius of the polygon/edge shape skin. This should not be modified. Making
        /// this smaller means polygons will have an insufficient buffer for continuous collision.
        /// Making it larger may create artifacts for vertex collision.
        /// </summary>
        /// <remarks>
        ///     Default is set to be 2 x linearslop.
        /// </remarks>
        public const float PolygonRadius = 2 * LinearSlop;

        /// <summary>
        /// Minimum buffer distance for positions.
        /// </summary>
        public const float LinearSlop = 0.005f;

        /// <summary>
        /// Minimum buffer distance for angles.
        /// </summary>
        public const float AngularSlop = 2.0f / 180.0f * MathF.PI;
        
        public const byte MaxPolygonVertices = 8;

        public const float DefaultContactFriction = 0.4f;

        public const float DefaultRestitution = 0f;

        public const float DefaultDensity = 1f;
    }
}
