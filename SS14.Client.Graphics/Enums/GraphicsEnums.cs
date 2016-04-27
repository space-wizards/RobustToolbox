
using System;

namespace SS14.Client.Graphics
{
    /// <summary>
    /// Enumeration for primitive drawing style.
    /// </summary>
    public enum PrimitiveStyle
	{
		/// <summary>A series of individual points.</summary>
		PointList = 0,
		/// <summary>A series of individual lines.</summary>
		LineList = 1,
		/// <summary>A series of lines connected in a strip.</summary>
		LineStrip = 2,
		/// <summary>A series of individual triangles.</summary>
		TriangleList = 3,
		/// <summary>A series of triangles connected in a strip.</summary>
		TriangleStrip = 4,
		/// <summary>A series of triangles connected in a fan.</summary>
		TriangleFan = 5
	}

	/// <summary>
	/// Enumeration for culling modes.
	/// </summary>
	public enum CullingMode
	{
		/// <summary>Cull counter clockwise.</summary>
		CounterClockwise = 0,
		/// <summary>Cull clockwise.</summary>
		Clockwise = 1,
		/// <summary>No culling.</summary>
		None = 2,
	}

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
    /// Enumeration containing image operation flags.
    /// </summary>
    public enum ImageOperations
    {
        /// <summary>Disable the image layer.</summary>
        Disable = 0,
        /// <summary>Select the previous first operation argument.</summary>
        SelectArgument1 = 1,
        /// <summary>Select the previous second operation argument.</summary>
        SelectArgument2 = 2,
        /// <summary>Modulate the layer.</summary>
        Modulate = 3,
        /// <summary>Modulate the layer times 2.</summary>
        Modulatex2 = 4,
        /// <summary>Modulate the layer times 4.</summary>
        Modulatex4 = 5,
        /// <summary>Additive blending.</summary>
        Additive = 6,
        /// <summary>Additive blending with sign.</summary>
        AdditiveSigned = 7,
        /// <summary>Additive blending with sign times 2.</summary>
        AdditiveSignedx2 = 8,
        /// <summary>Additive blending with interpolation.</summary>
        AdditiveSmooth = 9,
        /// <summary>Subtract blending.</summary>
        Subtract = 10,
        /// <summary>Blend diffuse and alpha.</summary>
        BlendDiffuseAlpha = 11,
        /// <summary>Blend texture alpha.</summary>
        BlendTextureAlpha = 12,
        /// <summary>Blend factor alpha.</summary>
        BlendFactorAlpha = 13,
        /// <summary>Blend pre-multipled alpha.</summary>
        BlendPreMultipliedTextureAlpha = 14,
        /// <summary>Blend current alpha.</summary>
        BlendCurrentAlpha = 15,
        /// <summary>Pre modulate.</summary>
        PreModulate = 16,
        /// <summary>Modulate alpha, additive blending.</summary>
        ModulateAlphaAddColor = 17,
        /// <summary>Modulate color, additive alpha.</summary>
        ModulateColorAddAlpha = 18,
        /// <summary>Modulate inverse alpha, additive color.</summary>
        ModulateInverseAlphaAddColor = 19,
        /// <summary>Modulate inverse color, additive alpha.</summary>
        ModulateInverseColorAddAlpha = 20,
        /// <summary>Bump mapping using an environment map.</summary>
        BumpEnvironmentMap = 21,
        /// <summary>Bump mapping using an environment map with luminance.</summary>
        BumpEnvironmentMapLuminance = 22,
        /// <summary>Bump mapping using Dot3 product.</summary>
        BumpDotProduct = 23,
        /// <summary>Multiply and Add.</summary>
        MultiplyAdd = 24,
        /// <summary>Linear interpolation.</summary>
        Lerp = 25
    }

    /// <summary>
    /// Enumeration containing arguments for image operations.
    /// </summary>
    public enum ImageOperationArguments
    {
        /// <summary>Get the current setting.</summary>
        Current = 0,
        /// <summary>Diffuse color from the vertex.</summary>
        Diffuse = 1,
        /// <summary>Texture color from the layer.</summary>
        Texture = 2,
        /// <summary>Texture factor.</summary>
        TextureFactor = 3,
        /// <summary>Temporary register.</summary>
        Temp = 4,
        /// <summary>Constant value.</summary>
        Constant = 5,
        /// <summary>Copy alpha values to color values.</summary>
        AlphaReplicate = 6,
        /// <summary>One's complement.</summary>
        Complement = 7,
        /// <summary>Specular value from the vertex.</summary>
        Specular = 8
    }

    /// <summary>
    /// Enumeration containing filter types for images.
    /// </summary>
    public enum ImageFilters
    {
        /// <summary>No filtering.</summary>
        None = 0,
        /// <summary>Point filtering.</summary>
        Point = 1,
        /// <summary>Bilinear filtering.</summary>
        Bilinear = 2,
        /// <summary>Anisotropic filtering.</summary>
        Anisotropic = 3,
        /// <summary>Pyramidal quadratic filtering.</summary>
        PyramidalQuadratic = 4,
        /// <summary>Gaussian quadratic filtering.</summary>
        GaussianQuadratic = 5
    }

    /// <summary>
    /// Enumeration containing the types of images we can create.
    /// </summary>
	/// <remarks>The RenderTarget value is used internally by Gorgon and should not be used when creating an image, however it can be used when validating an image format with 
	/// <see cref="M:GorgonLibrary.Driver.ValidImageFormat">Driver.ValidImageFormat</see> or <see cref="M:GorgonLibrary.Graphics.Image.ValidateFormat">Image.ValidateFormat</see>.
	/// <para>The Dynamic value will ensure the image is dynamic whether the <see cref="P:GorgonLibrary.Driver.SupportDynamicTextures">Driver.SupportDynamicTextures</see> property is TRUE or FALSE.  
	/// If the hardware supports dynamic textures then Gorgon will make use of it, otherwise the image will be a normal image placed in the default pool.</para>
	/// </remarks>
    public enum ImageType
    {
        /// <summary>A normal static image.</summary>
        Normal = 0,
        /// <summary>Dynamic image.</summary>
        Dynamic = 1,
		/// <summary>
		/// A render target image.
		/// </summary>
		RenderTarget = 0x7FFF
    }

    /// <summary>
    /// Enumeration containing the types of image addressing modes.
    /// </summary>
    public enum ImageAddressing
    {
        /// <summary>Make image wrap around.</summary>
        Wrapping = 0,
        /// <summary>Make image mirror.</summary>
        Mirror = 1,
        /// <summary>Make image mirror only once.</summary>
        MirrorOnce = 2,
        /// <summary>Make image clamp to values..</summary>
        Clamp = 3,
        /// <summary>Display a border.</summary>
        Border = 4
    }

    /// <summary>
    /// Enumeration containing image file formats.
    /// </summary>
    public enum ImageFileFormat
    {
        /// <summary>
        /// Windows bitmap format.
        /// </summary>
        BMP = 0,
        /// <summary>
        /// Joint photographers expert group.
        /// </summary>
        JPEG = 1,
        /// <summary>
        /// Portable network graphics.
        /// </summary>
        PNG = 2,
        /// <summary>
        /// Truevision Targa.
        /// </summary>
        TGA = 3,
        /// <summary>
        /// Direct X surface.
        /// </summary>
        DDS = 4,
        /// <summary>
        /// Device independant bitmap.
        /// </summary>
        DIB = 5,
        /// <summary>
        /// Portable pixmap. 
        /// </summary>
        PPM = 6,
        /// <summary>
        /// Portable floating point.
        /// </summary>
        PFM = 7
    }

    /// <summary>
    /// Enumeration containing locations of the vertices.
    /// </summary>
    public enum VertexLocations
    {
        /// <summary>Upper left corner.</summary>
        UpperLeft = 0,
        /// <summary>Lower left corner.</summary>
        LowerLeft = 3,
        /// <summary>Upper right corner.</summary>
        UpperRight = 1,
        /// <summary>Lower right corner.</summary>
        LowerRight = 2
    }

    /// <summary>
    /// Enumeration containing blending modes.
    /// </summary>
    [Flags()]
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

    /// <summary>
    /// Enumeration for smoothing operations.
    /// </summary>
    public enum Smoothing
    {
        /// <summary>No smoothing.</summary>
        None = 0,
        /// <summary>Smooth both zoomed in and out.</summary>
        Smooth = 1,
        /// <summary>Smooth only zoomed in.</summary>
        MagnificationSmooth = 2,
        /// <summary>Smooth only zoomed out.</summary>
        MinificationSmooth = 3
    }

    /// <summary>
    /// Enumeration for aligment.
    /// </summary>
    public enum Alignment
    {
        /// <summary>Left aligned.</summary>
        Left = 0,
        /// <summary>Centered.</summary>
        Center = 1,
        /// <summary>Right aligned.</summary>
        Right = 2,
        /// <summary>Upper left corner.</summary>
        UpperLeft = 3,
        /// <summary>Upper centered.</summary>
        UpperCenter = 4,
        /// <summary>Upper right.</summary>
        UpperRight = 5,
        /// <summary>Lower left.</summary>
        LowerLeft = 6,
        /// <summary>Lower centered.</summary>
        LowerCenter = 7,
        /// <summary>Lower right.</summary>
        LowerRight = 8
    }

    /// <summary>
    /// Enumeration for stencil buffer operations.
    /// </summary>
    public enum StencilOperations
    {
        /// <summary>Write zero to the buffer.</summary>
        Zero = 0,
        /// <summary>Decrement buffer value.</summary>
        Decrement = 1,
        /// <summary>Increment buffer value.</summary>
        Increment = 2,
        /// <summary>Invert buffer value.</summary>
        Invert = 3,
        /// <summary>Decrement buffer value, clamp to minimum.</summary>
        DecrementSaturate = 4,
        /// <summary>Increment buffer value, clamp to maximum.</summary>
        IncrementSaturate = 5,
        /// <summary>Keep the current value.</summary>
        Keep = 6,
        /// <summary>Replace the value with the reference value.</summary>
        Replace = 7
    }

    /// <summary>
    /// Enumeration for alpha blending operations.
    /// </summary>
    public enum AlphaBlendOperation
    {
        /// <summary>Blend factor of 0,0,0.</summary>
        Zero = 0,
        /// <summary>Blend factor is 1,1,1.</summary>
        One = 1,
        /// <summary>Blend factor is Rs', Gs', Bs', As.</summary>
        SourceColor = 2,
        /// <summary>Blend factor is As', As', As', As.</summary>
        SourceAlpha = 3,
        /// <summary>Blend factor is 1-Rs', 1-Gs', 1-Bs', 1-As.</summary>
        InverseSourceColor = 4,
        /// <summary>Blend factor is 1-As', 1-As', 1-As', 1-As.</summary>
        InverseSourceAlpha = 5,
        /// <summary>Blend factor is Rd', Gd', Bd', Ad.</summary>
        DestinationColor = 6,
        /// <summary>Blend factor is Ad', Ad', Ad', Ad.</summary>
        DestinationAlpha = 7,
        /// <summary>Blend factor is 1-Rd', 1-Gd', 1-Bd', 1-Ad.</summary>
        InverseDestinationColor = 8,
        /// <summary>Blend factor is 1-Ad', 1-Ad', 1-Ad', 1-Ad.</summary>
        InverseDestinationAlpha = 9,
        /// <summary>Blend factor is f,f,f,1 where f = min(A, 1-Ad)</summary>
        SourceAlphaSaturation = 10,
        /// <summary>Source blend factor is 1-As', 1-As', 1-As', 1-As and destination is As', As', As', As.  Overrides the blend destination, and is only valid if the SourceBlend state is true.</summary>
        BothInverseSourceAlpha = 11,
        /// <summary>Constant color blend factor.  Only valid if the driver SupportBlendingFactor is true.</summary>
        BlendFactor = 12,
        /// <summary>Inverted constant color blend factor.  Only valid if the driver SupportBlendingFactor capability is true.</summary>
        InverseBlendFactor = 13
    }
}

