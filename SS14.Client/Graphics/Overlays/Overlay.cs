using SS14.Client.Graphics.Drawing;
using SS14.Client.Graphics.Shaders;
using SS14.Client.Interfaces.Graphics.Overlays;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SS14.Client.Graphics.Clyde;
using SS14.Shared.Utility;
using VS = Godot.VisualServer;

namespace SS14.Client.Graphics.Overlays
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

        private Shader _shader;

        public Shader Shader
        {
            get => _shader;
            set
            {
                _shader = value;
                if (GameController.OnGodot && MainCanvasItem != null)
                {
                    VS.CanvasItemSetMaterial(MainCanvasItem, value?.GodotMaterial?.GetRid());
                }
            }
        }

        private int? _zIndex;
        public int? ZIndex
        {
            get => _zIndex;
            set
            {
                if (value != null && (_zIndex > VS.CanvasItemZMax || _zIndex < VS.CanvasItemZMin))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _zIndex = value;
                UpdateZIndex();
            }
        }

        public virtual bool SubHandlesUseMainShader { get; } = true;

        private bool _isDirty = true;

        private Godot.RID MainCanvasItem;

        private readonly List<Godot.RID> CanvasItems = new List<Godot.RID>();
        private readonly List<DrawingHandle> TempHandles = new List<DrawingHandle>();

        private bool Disposed;

        protected Overlay(string id)
        {
            OverlayManager = IoCManager.Resolve<IOverlayManager>();
            ID = id;
        }

        internal void AssignCanvasItem(Godot.RID canvasItem)
        {
            MainCanvasItem = canvasItem;
            Shader?.ApplyToCanvasItem(MainCanvasItem);
            UpdateZIndex();
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
            ClearDraw();
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
                case GameController.DisplayMode.Godot:
                {
                    var item = VS.CanvasItemCreate();
                    VS.CanvasItemSetParent(item, MainCanvasItem);
                    CanvasItems.Add(item);
                    if (shader != null)
                    {
                        shader.ApplyToCanvasItem(item);
                    }
                    else
                    {
                        VS.CanvasItemSetUseParentMaterial(item, SubHandlesUseMainShader);
                    }

                    DrawingHandle handle;
                    switch (Space)
                    {
                        case OverlaySpace.ScreenSpaceBelowWorld:
                        case OverlaySpace.ScreenSpace:
                            handle = new DrawingHandleScreen(item);
                            break;
                        case OverlaySpace.WorldSpace:
                            handle = new DrawingHandleWorld(item);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    TempHandles.Add(handle);
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
            if (!IsDirty || !GameController.OnGodot)
            {
                return;
            }

            ClearDraw();

            try
            {
                Drawing = true;
                DrawingHandle handle;
                switch (Space)
                {
                    case OverlaySpace.ScreenSpaceBelowWorld:
                    case OverlaySpace.ScreenSpace:
                        handle = new DrawingHandleScreen(MainCanvasItem);
                        break;
                    case OverlaySpace.WorldSpace:
                        handle = new DrawingHandleWorld(MainCanvasItem);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                Draw(handle);
            }
            finally
            {
                Drawing = false;
                foreach (var handle in TempHandles)
                {
                    handle.Dispose();
                }

                TempHandles.Clear();
            }
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

        private void ClearDraw()
        {
            if (!GameController.OnGodot)
            {
                return;
            }

            foreach (var item in CanvasItems)
            {
                VS.FreeRid(item);
            }

            VS.CanvasItemClear(MainCanvasItem);

            CanvasItems.Clear();
        }

        private void UpdateZIndex()
        {
            if (MainCanvasItem == null || !GameController.OnGodot)
            {
                return;
            }

            if (Space != OverlaySpace.WorldSpace || ZIndex == null)
            {
                VS.CanvasItemSetZIndex(MainCanvasItem, 0);
                VS.CanvasItemSetZAsRelativeToParent(MainCanvasItem, true);
            }
            else
            {
                VS.CanvasItemSetZIndex(MainCanvasItem, ZIndex.Value);
                VS.CanvasItemSetZAsRelativeToParent(MainCanvasItem, false);
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
