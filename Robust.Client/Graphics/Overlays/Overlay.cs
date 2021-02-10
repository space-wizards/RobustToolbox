using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Shaders;
using Robust.Shared.IoC;
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Client.Graphics.Clyde;
using Robust.Client.Graphics.Interfaces.Graphics;
using Robust.Client.Graphics.Interfaces.Graphics.Overlays;
using Robust.Shared.Timing;

namespace Robust.Client.Graphics.Overlays
{
    /// <summary>
    ///     An overlay is used for fullscreen drawing in the game, for example parallax.
    /// </summary>
    [PublicAPI]
    public abstract class Overlay
    {
        /// <summary>
        ///     The ID of this overlay. This is used to identify it inside the <see cref="IOverlayManager"/>.
        /// </summary>
        public string ID { get; }

        public virtual bool AlwaysDirty => false;
        public bool IsDirty => AlwaysDirty || _isDirty;
        public bool Drawing { get; private set; }

        public virtual OverlaySpace Space => OverlaySpace.ScreenSpace;

        protected IOverlayManager OverlayManager { get; }

        public int? ZIndex { get; set; }

        public virtual bool SubHandlesUseMainShader { get; } = true;

        private bool _isDirty = true;

        private readonly List<DrawingHandleBase> TempHandles = new();

        private bool Disposed;

        protected Overlay(string id)
        {
            OverlayManager = IoCManager.Resolve<IOverlayManager>();
            ID = id;
        }

        public void Dispose()
        {
            if (Disposed)
            {
                return;
            }

            Dispose(true);
            Disposed = true;
            GC.SuppressFinalize(this);
        }

        ~Overlay()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// Draws this overlay to the current space.
        /// </summary>
        /// <param name="handle">Current drawing handle that the overlay should be drawing with. Do not hold a reference to this in the overlay.</param>
        /// <param name="currentSpace">Current space that is being drawn. This function is called for every space that was set up in initialization.</param>
        protected abstract void Draw(DrawingHandleBase handle, OverlaySpace currentSpace);

        public void Dirty()
        {
            _isDirty = true;
        }

        protected internal virtual void FrameUpdate(FrameEventArgs args) { }

        internal void ClydeRender(IRenderHandle renderHandle, OverlaySpace currentSpace)
        {
            DrawingHandleBase handle;
            if (currentSpace == OverlaySpace.WorldSpace)
                handle = renderHandle.DrawingHandleWorld;
            else
                handle = renderHandle.DrawingHandleScreen;

            Draw(handle, currentSpace);
        }
    }


    /// <summary>
    ///     Determines in which canvas layers an overlay gets drawn.
    /// </summary>
    [Flags]
    public enum OverlaySpace : byte
    {
        /// <summary>
        ///     Used for matching bit flags.
        /// </summary>
        None = 0b0000,

        /// <summary>
        ///     This overlay will be drawn in the UI root, thus being in screen space.
        /// </summary>
        ScreenSpace = 0b0001,

        /// <summary>
        ///     This overlay will be drawn in the world root, thus being in world space.
        /// </summary>
        WorldSpace = 0b0010,

        /// <summary>
        ///     Drawn in screen coordinates, but behind the world.
        /// </summary>
        ScreenSpaceBelowWorld = 0b0100,
    }
}
