
using System;

namespace SS14.Client.Graphics
{
    /// <summary>
	/// Enumerator for vertex field contexts.
	/// Used to define in which context the field will be used.
	/// </summary>
	public enum VertexFieldContext
	{
		/// Position, 3 reals per vertex.
		Position,
		/// Normal, 3 reals per vertex.
		Normal,
		/// Blending weights.
		BlendWeights,
		/// Blending indices.
		BlendIndices,
		/// Diffuse colors.
		Diffuse,
		/// Specular colors.
		Specular,
		/// Texture coordinates.
		TexCoords,
		/// Binormal (Y axis if normal is Z).
		Binormal,
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
		Float1,
		/// 2 Floating point numbers.
		Float2,
		/// 3 Floating point numbers.
		Float3,
		/// 4 Floating point numbers.
		Float4,
		/// DWORD color value.
		Color,
		/// 1 signed short integers.
		Short1,
		/// 2 signed short integers.
		Short2,
		/// 3 signed short integers.
		Short3,
		/// 4 signed short integers.
		Short4,
		/// 4 Unsigned bytes.
		UByte4
	}
	
	/// <summary>
    /// Enumeration containing modes for the blitters on the image/render image objects.
    /// </summary>
    public enum BlitterSizeMode
    {
        None = 0,
        /// <summary>
        /// Scale the image based on the width and height passed to the blitter.
        /// </summary>
        Scale = 1,
        /// <summary>
        /// Crop the image based on the width and height passed to the blitter.
        /// </summary>
        Crop = 2

    }

    /// <summary>
    /// Enumeration for image formats.
    /// </summary>
    public enum ImageBufferFormats
    {
        /// <summary>Unknown buffer format.</summary>
        BufferUnknown = 0,
        /// <summary>24 bit color.</summary>
        BufferRGB888 = 1,
        /// <summary>24 bit color, 8 bit alpha.</summary>
        BufferRGB888A8 = 2,
        /// <summary>15 bit color, 1 bit alpha.</summary>
        BufferRGB555A1 = 3,
        /// <summary>12 bit color, 4 bit alpha.</summary>
        BufferRGB444A4 = 4,
        /// <summary>48 bit color (BGR), 16 bit alpha.</summary>
        BufferBGR161616A16 = 5,
        /// <summary>48 bit color (BGR), 16 bit alpha, floating point.</summary>
        BufferBGR161616A16F = 6,
        /// <summary>30 bit color, 2 bit alpha.</summary>
        BufferRGB101010A2 = 7,
        /// <summary>30 bit color (BGR), 2 bit alpha.</summary>
        BufferBGR101010A2 = 8,
        /// <summary>30 bit bump map (WVU), 2 bit alpha.</summary>
        BufferWVU101010A2 = 9,
        /// <summary>96 bit color (BGR), 32 bit alpha, floating point.</summary>
        BufferBGR323232A32F = 10,
        /// <summary>4 bit luminance, 4 bit alpha.</summary>
        BufferA4L4 = 11,
        /// <summary>8 bit alpha.</summary>
        BufferA8 = 12,
        /// <summary>8 bit color, 8 bit alpha.</summary>
        BufferRGB332A8 = 13,
        /// <summary>32 bit normal map, compressed.</summary>
        BufferVU88Cx = 14,
        /// <summary>DXT1 compression.</summary>
        BufferDXT1 = 15,
        /// <summary>DXT2 compression.</summary>
        BufferDXT2 = 16,
        /// <summary>DXT3 compression.</summary>
        BufferDXT3 = 17,
        /// <summary>DXT4 compression.</summary>
        BufferDXT4 = 18,
        /// <summary>DXT5 compression.</summary>
        BufferDXT5 = 19,
        /// <summary>32 bit color (GR).</summary>
        BufferGR1616 = 20,
        /// <summary>32 bit color (GR), floating point.</summary>
        BufferGR1616F = 21,
        /// <summary>16 bit RGB packed.</summary>
        BufferRGB888G8 = 22,
        /// <summary>16 bit luminance.</summary>
        BufferL16 = 23,
        /// <summary>16 bit bump map, 5 bits for VU, 6 bits for luminance.</summary>
        BufferVU55L6 = 24,
        /// <summary>8 bit luminance.</summary>
        BufferL8 = 25,
        /// <summary>Uncompressed multi element.</summary>
        BufferMulti2RGBA = 26,
        /// <summary>8 bit color, indexed.</summary>
        BufferP8 = 27,
        /// <summary>64 bit bumpmap.</summary>
        BufferQWVU16161616 = 28,
        /// <summary>32 bit bumpmap.</summary>
        BufferQWVU8888 = 29,
        /// <summary>16 bit color (R), floating point.</summary>
        BufferR16F = 30,
        /// <summary>32 bit color (R), floating point.</summary>
        BufferR32F = 31,
        /// <summary>8 bit color.</summary>
        BufferRGB332 = 32,
        /// <summary>32 bit bump map.</summary>
        BufferVU1616 = 34,
        /// <summary>16 bit bump map.</summary>
        BufferVU88 = 35,
        /// <summary>15 bit color.</summary>
        BufferRGB555X1 = 36,
        /// <summary>12 bit color.</summary>
        BufferRGB444X4 = 37,
        /// <summary>32 bit color.</summary>
        BufferRGB888X8 = 38,
        /// <summary>32 bit bumpmap.</summary>
        BufferLVU888X8 = 39,
        /// <summary>32 bit color (BGR).</summary>
        BufferBGR888X8 = 40,
        /// <summary>4 bit bumpmap.</summary>
        BufferYUY2 = 41,
        /// <summary>24 bit color, extra 8 bit green channel.</summary>
        BufferG8RGB888 = 42,
        /// <summary>8 bit alpha, 8 bit luminance.</summary>
        BufferA8L8 = 43,
        /// <summary>8 bit alpha, 8 bit indexed color.</summary>
        BufferA8P8 = 44,
        /// <summary>64 bit color (GR), floating point.</summary>
        BufferGR3232F = 45,
        /// <summary>16 bit color.</summary>
        BufferRGB565 = 46
    }

    /// <summary>
    /// Enumeration containing blending modes.
    /// </summary>
    [Flags]
    public enum BlendingModes
    {
        /// <summary>No blending.</summary>
        None = 0,
        /// <summary>Modulated blending.</summary>
        Modulated = 1,
        /// <summary>Additive blending.</summary>
        Additive = 2,
        /// <summary>Inverse modulated blending.</summary>
		ModulatedInverse = 4,
        /// <summary>Color blending.</summary>
        Color = 8,
        /// <summary>Additive color.</summary>
        ColorAdditive = 16,
        /// <summary>Use premultiplied.</summary>
        PreMultiplied = 32,
		/// <summary>Invert.</summary>
		Inverted = 64
    }
}

