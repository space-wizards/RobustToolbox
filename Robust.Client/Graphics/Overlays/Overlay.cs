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
        ///     If set to true, <see cref="ScreenTexture"/> will be set to the current frame. This can be costly to performance, but
        ///     some shaders will require it as a passed in uniform to operate.
        /// </summary>
        public virtual bool RequestScreenTexture => false;

        /// <summary>
        ///     If <see cref="RequestScreenTexture"> is true, then this will be set to the texture corresponding to the current frame. If false, it will always be null.
        /// </summary>
        public Texture? ScreenTexture = null;

        /// <summary>
        ///     If set to true, the results of this overlay will entirely overwrite the framebuffer it being overlayed onto. 
        /// </summary>
        public virtual bool OverwriteTargetFrameBuffer => false;

        /// <summary>
        ///    Overlays on the same OverlaySpace will be drawn from lowest ZIndex to highest ZIndex. As an example, ZIndex -1 will be drawn before ZIndex 2. 0 by default. Overlays with same ZIndex will be drawn in 
        /// </summary>
        public int? ZIndex { get; set; }

        protected IOverlayManager OverlayManager { get; }

        protected Overlay()
        {
            OverlayManager = IoCManager.Resolve<IOverlayManager>();
        }

        /// <summary>
        /// Draws this overlay to the current space.
        /// </summary>
        /// <param name="handle">Current drawing handle that the overlay should be drawing with. Do not hold a reference to this in the overlay.</param>
        /// <param name="currentSpace">Current space that is being drawn. This function is called for every space that was set up in initialization.</param>
        protected abstract void Draw(DrawingHandleBase handle, OverlaySpace currentSpace);

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
