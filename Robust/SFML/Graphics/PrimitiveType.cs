using System;
using System.Runtime.InteropServices;

namespace SFML
{
    namespace Graphics
    {
        ////////////////////////////////////////////////////////////
        /// <summary>
        /// Types of primitives that a VertexArray can render.
        ///
        /// Points and lines have no area, therefore their thickness
        /// will always be 1 pixel, regarldess the current transform
        /// and view.
        /// </summary>
        ////////////////////////////////////////////////////////////
        public enum PrimitiveType
        {
            /// List of individual points
            Points,

            /// List of individual lines
            Lines,

            /// List of connected lines, a point uses the previous point to form a line
            LinesStrip,

            /// List of individual triangles
            Triangles,

            /// List of connected triangles, a point uses the two previous points to form a triangle
            TrianglesStrip,

            /// List of connected triangles, a point uses the common center and the previous point to form a triangle
            TrianglesFan,

            /// List of individual quads
            Quads
        }
    }
}
