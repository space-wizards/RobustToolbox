namespace Robust.Client.Graphics
{
    /// <summary>
    ///     Provides frame statistics about rendering.
    /// </summary>
    internal interface IClydeDebugStats
    {
        /// <summary>
        ///     The amount of draw calls sent to OpenGL last frame.
        /// </summary>
        int LastGLDrawCalls { get; }

        /// <summary>
        ///     The amount of Clyde draw calls done last frame.
        /// </summary>
        /// <remarks>
        ///     This is stuff like <see cref="DrawingHandleScreen.DrawTexture"/>.
        /// </remarks>
        int LastClydeDrawCalls { get; }

        /// <summary>
        ///     The amount of batches made.
        /// </summary>
        int LastBatches { get; }

        (int vertices, int indices) LargestBatchSize { get; }

        /// <summary>
        ///     The amount of lights that were rendered last frame.
        /// </summary>
        int TotalLights { get; }
    }
}
