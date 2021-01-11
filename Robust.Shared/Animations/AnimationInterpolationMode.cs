namespace Robust.Shared.Animations
{
    /// <summary>
    ///     Specifies how animated properties are interpolated between two keyframes.
    /// </summary>
    public enum AnimationInterpolationMode: byte
    {
        /// <summary>
        ///     Use a linear interpolation for supported values.
        ///     For unsupported values, this falls back to <see cref="Previous"/>.
        /// </summary>
        Linear,

        /// <summary>
        ///     Use a cubic interpolation for supported values.
        ///     For unsupported values, this falls back to <see cref="Previous"/>.
        /// </summary>
        Cubic,

        /// <summary>
        ///     Use nearest neighbor as interpolation.
        /// </summary>
        /// <remarks>
        ///     Nearest neighbor discretely flips between the previous and next keyframe 50% in between the two.
        /// </remarks>
        Nearest,

        /// <summary>
        ///     Use the previous keyframe as value.
        /// </summary>
        Previous
    }
}
