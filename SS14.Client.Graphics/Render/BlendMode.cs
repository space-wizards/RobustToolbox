using System;
using System.Runtime.InteropServices;
using SBlendMode = SFML.Graphics.BlendMode;
using SFactor = SFML.Graphics.BlendMode.Factor;
using SEquation = SFML.Graphics.BlendMode.Equation;

// File mostly copy pasted from SFML.NET.
namespace SS14.Client.Graphics.Render
{
    /// <summary>
    /// Blending modes for drawing
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct BlendMode : IEquatable<BlendMode>
    {
        /// <summary>
        /// Enumeration of the blending factors
        /// </summary>
        public enum Factor
        {
            /// <summary>(0, 0, 0, 0)</summary>
            Zero,

            /// <summary>(1, 1, 1, 1)</summary>
            One,

            /// <summary>(src.r, src.g, src.b, src.a)</summary>
            SrcColor,

            /// <summary>(1, 1, 1, 1) - (src.r, src.g, src.b, src.a)</summary>
            OneMinusSrcColor,

            /// <summary>(dst.r, dst.g, dst.b, dst.a)</summary>
            DstColor,

            /// <summary>(1, 1, 1, 1) - (dst.r, dst.g, dst.b, dst.a)</summary>
            OneMinusDstColor,

            /// <summary>(src.a, src.a, src.a, src.a)</summary>
            SrcAlpha,

            /// <summary>(1, 1, 1, 1) - (src.a, src.a, src.a, src.a)</summary>
            OneMinusSrcAlpha,

            /// <summary>(dst.a, dst.a, dst.a, dst.a)</summary>
            DstAlpha,

            /// <summary>(1, 1, 1, 1) - (dst.a, dst.a, dst.a, dst.a)</summary>
            OneMinusDstAlpha
        }

        /// <summary>
        /// Enumeration of the blending equations
        /// </summary>
        public enum Equation
        {
            /// <summary>Pixel = Src * SrcFactor + Dst * DstFactor</summary>
            Add,

            /// <summary>Pixel = Src * SrcFactor - Dst * DstFactor</summary>
            Subtract,

            /// <summary>Pixel = Dst * DstFactor - Src * SrcFactor</summary>
            ReverseSubtract
        }

        /// <summary>Blend source and dest according to dest alpha</summary>
        public static readonly BlendMode Alpha = new BlendMode(Factor.SrcAlpha, Factor.OneMinusSrcAlpha, Equation.Add,
                                                               Factor.One, Factor.OneMinusSrcAlpha, Equation.Add);

        /// <summary>Add source to dest</summary>
        public static readonly BlendMode Add = new BlendMode(Factor.SrcAlpha, Factor.One, Equation.Add,
                                                             Factor.One, Factor.One, Equation.Add);

        /// <summary>Multiply source and dest</summary>
        public static readonly BlendMode Multiply = new BlendMode(Factor.DstColor, Factor.Zero);

        /// <summary>Overwrite dest with source</summary>
        public static readonly BlendMode None = new BlendMode(Factor.One, Factor.Zero);


        /// <summary>
        /// Construct the blend mode given the factors and equation
        /// </summary>
        /// <param name="SourceFactor">Specifies how to compute the source factor for the color and alpha channels.</param>
        /// <param name="DestinationFactor">Specifies how to compute the destination factor for the color and alpha channels.</param>
        public BlendMode(Factor SourceFactor, Factor DestinationFactor)
            : this(SourceFactor, DestinationFactor, Equation.Add)
        {
        }

        /// <summary>
        /// Construct the blend mode given the factors and equation
        /// </summary>
        /// <param name="SourceFactor">Specifies how to compute the source factor for the color and alpha channels.</param>
        /// <param name="DestinationFactor">Specifies how to compute the destination factor for the color and alpha channels.</param>
        /// <param name="BlendEquation">Specifies how to combine the source and destination colors and alpha.</param>
        public BlendMode(Factor SourceFactor, Factor DestinationFactor, Equation BlendEquation)
            : this(SourceFactor, DestinationFactor, BlendEquation, SourceFactor, DestinationFactor, BlendEquation)
        {
        }

        /// <summary>
        /// Construct the blend mode given the factors and equation
        /// </summary>
        /// <param name="ColorSourceFactor">Specifies how to compute the source factor for the color channels.</param>
        /// <param name="ColorDestinationFactor">Specifies how to compute the destination factor for the color channels.</param>
        /// <param name="ColorBlendEquation">Specifies how to combine the source and destination colors.</param>
        /// <param name="AlphaSourceFactor">Specifies how to compute the source factor.</param>
        /// <param name="AlphaDestinationFactor">Specifies how to compute the destination factor.</param>
        /// <param name="AlphaBlendEquation">Specifies how to combine the source and destination alphas.</param>
        public BlendMode(Factor ColorSourceFactor, Factor ColorDestinationFactor, Equation ColorBlendEquation, Factor AlphaSourceFactor, Factor AlphaDestinationFactor, Equation AlphaBlendEquation)
        {
            ColorSrcFactor = ColorSourceFactor;
            ColorDstFactor = ColorDestinationFactor;
            ColorEquation = ColorBlendEquation;
            AlphaSrcFactor = AlphaSourceFactor;
            AlphaDstFactor = AlphaDestinationFactor;
            AlphaEquation = AlphaBlendEquation;
        }

        /// <summary>
        /// Compare two blend modes and checks if they are equal
        /// </summary>
        /// <returns>Blend Modes are equal</returns>
        public static bool operator ==(BlendMode left, BlendMode right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compare two blend modes and checks if they are not equal
        /// </summary>
        /// <returns>Blend Modes are not equal</returns>
        public static bool operator !=(BlendMode left, BlendMode right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Compare blend mode and object and checks if they are equal
        /// </summary>
        /// <param name="obj">Object to check</param>
        /// <returns>Object and blend mode are equal</returns>
        public override bool Equals(object obj)
        {
            return (obj is BlendMode) && Equals((BlendMode)obj);
        }

        /// <summary>
        /// Compare two blend modes and checks if they are equal
        /// </summary>
        /// <param name="other">Blend Mode to check</param>
        /// <returns>blend modes are equal</returns>
        public bool Equals(BlendMode other)
        {
            return (ColorSrcFactor == other.ColorSrcFactor) &&
                   (ColorDstFactor == other.ColorDstFactor) &&
                   (ColorEquation == other.ColorEquation) &&
                   (AlphaSrcFactor == other.AlphaSrcFactor) &&
                   (AlphaDstFactor == other.AlphaDstFactor) &&
                   (AlphaEquation == other.AlphaEquation);
        }

        /// <summary>
        /// Provide a integer describing the object
        /// </summary>
        /// <returns>Integer description of the object</returns>
        public override int GetHashCode()
        {
            return ColorSrcFactor.GetHashCode() ^
                   ColorDstFactor.GetHashCode() ^
                   ColorEquation.GetHashCode() ^
                   AlphaSrcFactor.GetHashCode() ^
                   AlphaDstFactor.GetHashCode() ^
                   AlphaEquation.GetHashCode();
        }

        /// <summary>Source blending factor for the color channels</summary>
        public Factor ColorSrcFactor { get; set; }

        /// <summary>Destination blending factor for the color channels</summary>
        public Factor ColorDstFactor { get; set; }

        /// <summary>Blending equation for the color channels</summary>
        public Equation ColorEquation { get; set; }

        /// <summary>Source blending factor for the alpha channel</summary>
        public Factor AlphaSrcFactor { get; set; }

        /// <summary>Destination blending factor for the alpha channel</summary>
        public Factor AlphaDstFactor { get; set; }

        /// <summary>Blending equation for the alpha channel</summary>
        public Equation AlphaEquation { get; set; }

        public static explicit operator SBlendMode(BlendMode mode)
        {
            var smode = new SBlendMode();
            smode.AlphaDstFactor = (SFactor)mode.AlphaDstFactor;
            smode.AlphaEquation = (SEquation)mode.AlphaEquation;
            smode.AlphaSrcFactor = (SFactor)mode.AlphaSrcFactor;
            smode.ColorDstFactor = (SFactor)mode.ColorDstFactor;
            smode.ColorEquation = (SEquation)mode.ColorEquation;
            smode.ColorSrcFactor = (SFactor)mode.ColorSrcFactor;
            return smode;
        }
    }
}
