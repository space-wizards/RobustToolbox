using System;

namespace SS14.Client.Interfaces.Graphics.Overlays
{
    public interface IOverlay : IDisposable
    {
        string ID { get; }

        OverlaySpace Space { get; }

        void FrameUpdate(RenderFrameEventArgs args);

        void AssignCanvasItem(Godot.RID canvasItem);
        void ClearCanvasItem();
    }

    /// <summary>
    ///     Determines in which canvas layer an overlay gets drawn.
    /// </summary>
    public enum OverlaySpace
    {
        /// <summary>
        ///     This overlay will be drawn in the UI root, thus being in screen space.
        /// </summary>
        ScreenSpace = 0,

        /// <summary>
        ///     This overlay will be drawn in the world root, thus being in world space.
        /// </summary>
        WorldSpace = 1,
    }
}
