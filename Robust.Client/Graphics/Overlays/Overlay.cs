using Robust.Shared.IoC;
using JetBrains.Annotations;
using Robust.Shared.Timing;
using Robust.Shared.Enums;
using System;
using Robust.Shared.Graphics;

namespace Robust.Client.Graphics
{
    /// <summary>
    ///     An overlay is used for fullscreen drawing in the game. This can range from drawing parallax to a full screen shader.
    /// </summary>
    [PublicAPI]
    public abstract class Overlay
    {
        /// <summary>
        ///     Determines when this overlay is drawn in the rendering queue.
        /// </summary>
        public virtual OverlaySpace Space => OverlaySpace.ScreenSpace;

        /// <summary>
        ///     If set to true, <see cref="ScreenTexture"/> will be set to the current frame (at the moment before the overlay is rendered). This can be costly to performance, but
        ///     some shaders will require it as a passed in uniform to operate.
        /// </summary>
        public virtual bool RequestScreenTexture { get; set; } = false;

        /// <summary>
        ///     If <see cref="RequestScreenTexture"> is true, then this will be set to the texture corresponding to the current frame. If false, it will always be null.
        /// </summary>
        public Texture? ScreenTexture = null;

        /// <summary>
        ///    Overlays on the same OverlaySpace will be drawn from lowest ZIndex to highest ZIndex. As an example, ZIndex -1 will be drawn before ZIndex 2.
        ///    This value is 0 by default. Overlays with same ZIndex will be drawn in an random order.
        /// </summary>
        public int? ZIndex { get; set; }

        protected IOverlayManager OverlayManager { get; }

        private bool Disposed = false;

        public Overlay()
        {
            OverlayManager = IoCManager.Resolve<IOverlayManager>();
        }

        /// <summary>
        ///     If this is true, the target framebuffer will be wiped before applying this overlay to it.
        /// </summary>
        public virtual bool OverwriteTargetFrameBuffer => false;

        /// <summary>
        /// Draws this overlay to the current space.
        /// </summary>
        protected internal abstract void Draw(in OverlayDrawArgs args);

        protected internal virtual void FrameUpdate(FrameEventArgs args) { }

        public void Dispose() {
            if (Disposed)
                return;
            else
                DisposeBehavior();
        }

        protected virtual void DisposeBehavior(){
            Disposed = true;
        }

        /// <summary>
        /// This function gets called prior to the overlay being drawn. If this function returns false, the overlay will
        /// not get drawn to this view-port. Useful for avoiding unnecessary screen-texture fetching or frame buffer
        /// clearing.
        /// </summary>
        /// <remarks>
        /// If you do not use <see cref="RequestScreenTexture"/> or <see cref="OverwriteTargetFrameBuffer"/>, you don't
        /// need to use this and can just perform these checks inside of <see cref="Draw"/> instead.
        /// </remarks>
        protected internal virtual bool BeforeDraw(in OverlayDrawArgs args)
        {
            return true;
        }
    }
}
