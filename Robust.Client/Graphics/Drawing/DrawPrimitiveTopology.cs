namespace Robust.Client.Graphics
{
    /// <summary>
    ///     Determines the type of primitives drawn and how they are laid out from vertices.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://www.khronos.org/registry/vulkan/specs/1.2-extensions/html/vkspec.html#drawing-point-lists">Vulkan's documentation</see> for descriptions of all these modes.
    /// </remarks>
    public enum DrawPrimitiveTopology : byte
    {
        PointList,
        TriangleList,
        TriangleFan,
        TriangleStrip,
        LineList,
        LineStrip
    }
}
