using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS14.Client.Graphics.VertexData
{
    public class VertexEnums
    {
        public enum VertexFieldContext
        {
            /// Position, 3 reals per vertex.
            Position ,
            /// Normal, 3 reals per vertex.
            Normal ,
            /// Blending weights.
            BlendWeights ,
            /// Blending indices.
            BlendIndices ,
            /// Diffuse colors.
            Diffuse ,
            /// Specular colors.
            Specular ,
            /// Texture coordinates.
            TexCoords ,
            /// Binormal (Y axis if normal is Z).
            Binormal ,
            /// Tangent (X axis if normal is Z).
            Tangent
        }


        /// <summary>
        /// Enumerator for vertex field types.
        /// Used to define what type of field we're using.
        /// </summary>
        public enum VertexFieldType
        {
            /// 1 Floating point number.
            Float1 ,
            /// 2 Floating point numbers.
            Float2 ,
            /// 3 Floating point numbers.
            Float3 ,
            /// 4 Floating point numbers.
            Float4 ,
            /// DWORD color value.
            Color ,
            /// 1 signed short integers.
            Short1 ,
            /// 2 signed short integers.
            Short2 ,
            /// 3 signed short integers.
            Short3 ,
            /// 4 signed short integers.
            Short4 ,
            /// 4 Unsigned bytes.
            UByte4
        }
    }
}
