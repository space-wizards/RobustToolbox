using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Shared.IoC;
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Client.Graphics.Clyde;
using Robust.Shared.Utility;

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

        private IRenderHandle _renderHandle;

        public Shader Shader { get; set; }

        public int? ZIndex { get; set; }

        public virtual bool SubHandlesUseMainShader { get; } = true;

        private bool _isDirty = true;

        private readonly List<DrawingHandle> TempHandles = new List<DrawingHandle>();

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

        protected abstract void Draw(DrawingHandle handle);

        protected DrawingHandle NewHandle(Shader shader = null)
        {
            if (!Drawing)
            {
                throw new InvalidOperationException("Can only allocate new handles while drawing.");
            }

            switch (GameController.Mode)
            {
                case GameController.DisplayMode.Headless:
                {
                    DrawingHandle handle;
                    switch (Space)
                    {
                        case OverlaySpace.ScreenSpaceBelowWorld:
                        case OverlaySpace.ScreenSpace:
                            handle = new DrawingHandleScreen();
                            break;
                        case OverlaySpace.WorldSpace:
                            handle = new DrawingHandleWorld();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    TempHandles.Add(handle);
                    return handle;
                }
                case GameController.DisplayMode.Clyde:
                {
                    DrawingHandle handle;
                    switch (Space)
                    {
                        case OverlaySpace.ScreenSpaceBelowWorld:
                        case OverlaySpace.ScreenSpace:
                            handle = _renderHandle.CreateHandleScreen();
                            break;
                        case OverlaySpace.WorldSpace:
                            handle = _renderHandle.CreateHandleWorld();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    if (shader != null)
                    {
                        handle.UseShader(shader);
                    }

                    return handle;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Dirty()
        {
            _isDirty = true;
        }

        internal void FrameUpdate(RenderFrameEventArgs args)
        {
        }

        internal void OpenGLRender(IRenderHandle renderHandle)
        {
            DebugTools.Assert(GameController.Mode == GameController.DisplayMode.Clyde);

            try
            {
                _renderHandle = renderHandle;
                Drawing = true;

                var handle = NewHandle();
                if (Shader != null)
                {
                    handle.UseShader(Shader);
                }
                Draw(handle);
            }
            finally
            {
                _renderHandle = null;
                Drawing = false;
            }
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
