using OpenTK;
using SFML.Graphics;
using SFML.System;
using SS14.Client.Graphics.Collection;
using SS14.Client.Graphics.Utility;
using SS14.Shared.Maths;
using System;
using System.Runtime.InteropServices;
using Color = SS14.Shared.Maths.Color;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Client.Graphics.VertexData
{
    /// <summary>
    /// Object representing a list of vertex types.
    /// </summary>
    public class VertexTypeList : BaseCollection<VertexType>, IDisposable
    {
        #region Value Types.
        /// <summary>
        /// Value type describing a sprite vertex.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PositionDiffuse2DTexture1
        {
            private static uint ColorToInt(Color color)
                => unchecked((uint)(
                    (color.RByte << 16)
                    | (color.GByte << 8)
                    | (color.BByte)
                    | (color.AByte << 24)));

            private static Color IntToColor(uint color)
                => unchecked(new Color(
                    (byte)(color >> 16),
                    (byte)(color >> 8),
                    (byte)(color),
                    (byte)(color >> 24)));

            #region Variables.
            /// <summary>
            /// Position of the vertex.
            /// </summary>
            public Vector3 Position { get; set; }
            
            /// <summary>
            /// Color value of the vertex.
            /// </summary>
            internal uint ColorValue;

            /// <summary>
            /// Texture coordinates.
            /// </summary>
            public Vector2 TextureCoordinates { get; set; }
            #endregion Variables.

            #region Properties.
            /// <summary>
            /// Property to set or return the color as a <see cref="SFML.Graphics.Color"/> value.
            /// </summary>
            public Color Color
            {
                get
                {
                    return IntToColor(ColorValue);
                }
                set
                {
                    ColorValue = ColorToInt(value);
                }
            }
            
            #endregion Properties.

            #region Methods.
            /// <summary>
            /// Returns the fully qualified type name of this instance.
            /// </summary>
            /// <returns>
            /// A <see cref="T:System.String"/> containing a fully qualified type name.
            /// </returns>
            public override string ToString()
            {
                return string.Format("PositionDiffuse2DTexture1:\nPosition: X={0}, Y={1}, Z={2}\nDiffuse: R={3}, G={4}, B={5}, A={6}\n2D Texture coordinates (index 0): X={7}, Y={8}", Position.X, Position.Y, Position.Z, Color.R, Color.G, Color.B, Color.A, TextureCoordinates.X, TextureCoordinates.Y);
            }

            /// <summary>
            /// Indicates whether this instance and a specified object are equal.
            /// </summary>
            /// <param name="obj">Another object to compare to.</param>
            /// <returns>
            /// true if <paramref name="obj"/> and this instance are the same type and represent the same value; otherwise, false.
            /// </returns>
            public override bool Equals(object obj)
            {
                if (!(obj is PositionDiffuse2DTexture1))
                    return false;

                PositionDiffuse2DTexture1 value = (PositionDiffuse2DTexture1)obj;

                return (value.ColorValue == this.ColorValue) && (value.Position == this.Position) && (value.TextureCoordinates == this.TextureCoordinates);
            }

            /// <summary>
            /// Returns the hash code for this instance.
            /// </summary>
            /// <returns>
            /// A 32-bit signed integer that is the hash code for this instance.
            /// </returns>
            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            /// <summary>
            /// Implements the operator ==.
            /// </summary>
            /// <param name="left">The left value.</param>
            /// <param name="right">The right value.</param>
            /// <returns>The result of the operator.</returns>
            public static bool operator ==(PositionDiffuse2DTexture1 left, PositionDiffuse2DTexture1 right)
            {
                return (left.Position == right.Position) && (left.TextureCoordinates == right.TextureCoordinates) && (left.ColorValue == right.ColorValue);
            }

            /// <summary>
            /// Implements the operator !=.
            /// </summary>
            /// <param name="left">The left value.</param>
            /// <param name="right">The right value.</param>
            /// <returns>The result of the operator.</returns>
            public static bool operator !=(PositionDiffuse2DTexture1 left, PositionDiffuse2DTexture1 right)
            {
                return (left.Position != right.Position) || (left.TextureCoordinates != right.TextureCoordinates) || (left.ColorValue != right.ColorValue);
            }
            #endregion Methods.

            #region Constructor.
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="position">Position of the vertex.</param>
            /// <param name="color">Color of the vertex.</param>
            /// <param name="textureCoordinates">Texture coordinates.</param>
            public PositionDiffuse2DTexture1(Vector3 position, Color color, Vector2 textureCoordinates) : this()
            {
                // Copy data.
                Position = position;
                ColorValue = ColorToInt(color);
                TextureCoordinates = textureCoordinates;
            }
            #endregion Constructor.
        }
        #endregion Value Types.

        #region Properties.
        /// <summary>
        /// Property to return a vertex type by index.
        /// </summary>
        public VertexType this[int index]
        {
            get
            {
                return GetItem(index);
            }
        }

        /// <summary>
        /// Property to return a vertex type by its key name.
        /// </summary>
        public VertexType this[string key]
        {
            get
            {
                return GetItem(key);
            }
        }
        #endregion Properties.

        #region Methods.
        /// <summary>
        /// Function to clear the list.
        /// </summary>
        protected void Clear()
        {
            // Destroy all the vertex types.
            foreach (VertexType vertexType in this)
                vertexType.Dispose();

            base.ClearItems();
        }

        /// <summary>
        /// Function to create the vertex types.
        /// </summary>
        protected void CreateVertexTypes()
        {
            VertexType newType;		// Vertex type.

            // Position, Diffuse, Normal, 1 2D Texture Coord.
            newType = new VertexType();
            newType.CreateField(0, 0, VertexFieldContext.Position, VertexFieldType.Float3);
            newType.CreateField(0, 12, VertexFieldContext.Diffuse, VertexFieldType.Color);
            newType.CreateField(0, 16, VertexFieldContext.TexCoords, VertexFieldType.Float2);

            AddItem("PositionDiffuse2DTexture1", newType);
        }
        #endregion Methods.

        #region Constructor/Destructor.
        /// <summary>
        /// Constructor.
        /// </summary>
        internal VertexTypeList()
            : base(16, true)
        {
            CreateVertexTypes();
        }
        #endregion Constructor/Destructor.

        #region IDisposable Members

        /// <summary>
        /// Function to perform clean up.
        /// </summary>
        /// <param name="disposing">TRUE to release all resources, FALSE to only release unmanaged.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                Clear();
        }

        /// <summary>
        /// Function to perform clean up.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion IDisposable Members
    }
}
