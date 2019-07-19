using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Shared.IoC;
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Client.Graphics.Clyde;
using Robust.Client.Interfaces.Graphics;

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

        public ShaderInstance Shader { get; set; }

        public int? ZIndex { get; set; }

        public virtual bool SubHandlesUseMainShader { get; } = true;

        private bool _isDirty = true;

        private readonly List<DrawingHandleBase> TempHandles = new List<DrawingHandleBase>();

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

        protected abstract void Draw(DrawingHandleBase handle);

        public void Dirty()
        {
            _isDirty = true;
        }

        internal virtual void FrameUpdate(RenderFrameEventArgs args)
        {
        }

        internal void ClydeRender(IRenderHandle renderHandle)
        {
            DrawingHandleBase handle;
            if (Space == OverlaySpace.WorldSpace)
            {
                handle = renderHandle.DrawingHandleWorld;
            }
            else
            {
                handle = renderHandle.DrawingHandleScreen;
            }

            if (Shader != null)
            {
                handle.UseShader(Shader);
            }
            Draw(handle);
        }
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

        /// <summary>
        ///     Drawn in screen coordinates, but behind the world.
        /// </summary>
        ScreenSpaceBelowWorld = 2,
    }
}
