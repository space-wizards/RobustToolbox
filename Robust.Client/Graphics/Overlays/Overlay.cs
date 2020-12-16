using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Shared.IoC;
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.Timing;
using Robust.Shared.Enums;

namespace Robust.Client.Graphics.Overlays
{
    /// <summary>
    ///     An overlay is used for fullscreen drawing in the game, for example parallax.
    /// </summary>
    [PublicAPI]
    public abstract class Overlay
    {

        public virtual bool AlwaysDirty => false;
        public bool IsDirty => AlwaysDirty || _isDirty;
        public bool Drawing { get; private set; }

        public virtual OverlaySpace Space => OverlaySpace.ScreenSpace;

        public virtual OverlayPriority Priority => OverlayPriority.P5;

        /// <summary>
        ///     If set to true, <see cref="ScreenTexture"/> will be set to the current frame. This can be costly to performance, but
        ///     some shaders will require it as a passed in uniform to operate.
        /// </summary>
        public virtual bool RequestScreenTexture => false;

        /// <summary>
        ///     If <see cref="RequestScreenTexture"> is true, then this will be set to the texture corresponding to the current frame. If false, it will always be null.
        /// </summary>
        public Texture? ScreenTexture = null;

        /// <summary>
        ///     If set to true, the results of this shader will entirely overwrite the framebuffer it being overlayed onto. 
        /// </summary>
        public virtual bool OverwriteTargetFrameBuffer => false;

        protected IOverlayManager OverlayManager { get; }

        public int? ZIndex { get; set; }

        public virtual bool SubHandlesUseMainShader { get; } = true;

        private bool _isDirty = true;

        private readonly List<DrawingHandleBase> TempHandles = new();

        private bool Disposed;

        protected Overlay()
        {
            OverlayManager = IoCManager.Resolve<IOverlayManager>();
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
            ScreenTexture = null;
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
            if (currentSpace == OverlaySpace.ScreenSpace || currentSpace == OverlaySpace.ScreenSpaceBelowWorld)
                handle = renderHandle.DrawingHandleScreen;
            else
                handle = renderHandle.DrawingHandleWorld;

            Draw(handle, currentSpace);
        }
    }
}
