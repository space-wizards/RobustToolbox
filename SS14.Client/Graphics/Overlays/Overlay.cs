using SS14.Client.Graphics.Drawing;
using SS14.Client.Graphics.Shaders;
using SS14.Client.Interfaces.Graphics.Overlays;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if GODOT
using VS = Godot.VisualServer;
#endif

namespace SS14.Client.Graphics.Overlays
{
    public abstract class Overlay : IOverlay
    {
        protected IOverlayManager OverlayManager { get; }
        public string ID { get; }

        public virtual bool AlwaysDirty => false;
        public bool IsDirty => AlwaysDirty || _isDirty;
        public bool Drawing { get; private set; } = false;

        public virtual OverlaySpace Space => OverlaySpace.ScreenSpace;

        private Shader _shader;
        public Shader Shader
        {
            get => _shader;
            set
            {
                _shader = value;
                #if GODOT
                if (MainCanvasItem != null)
                {
                    VS.CanvasItemSetMaterial(MainCanvasItem, value?.GodotMaterial?.GetRid());
                }
                #endif
            }
        }

        private int? _zIndex;
        public int? ZIndex
        {
            get => _zIndex;
            set
            {
                #if GODOT
                if (value != null && (_zIndex > VS.CanvasItemZMax || _zIndex < VS.CanvasItemZMin))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                #endif
                _zIndex = value;
                UpdateZIndex();
            }
        }

        public virtual bool SubHandlesUseMainShader { get; } = true;

        private bool _isDirty = true;

        #if GODOT
        private Godot.RID MainCanvasItem;

        private readonly List<Godot.RID> CanvasItems = new List<Godot.RID>();
        #endif
        private readonly List<DrawingHandle> TempHandles = new List<DrawingHandle>();

        private bool Disposed = false;

        protected Overlay(string id)
        {
            OverlayManager = IoCManager.Resolve<IOverlayManager>();
            ID = id;
        }

        #if GODOT
        public void AssignCanvasItem(Godot.RID canvasItem)
        {
            MainCanvasItem = canvasItem;
            if (Shader != null)
            {
                Shader.ApplyToCanvasItem(MainCanvasItem);
            }
            UpdateZIndex();
        }
        #endif

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

            #if GODOT
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
            #endif

            DrawingHandle handle;
            switch (Space)
            {
                #if GODOT
                case OverlaySpace.ScreenSpace:
                    handle = new DrawingHandleScreen(item);
                    break;
                case OverlaySpace.WorldSpace:
                    handle = new DrawingHandleWorld(item);
                    break;
                #endif
                default:
                    throw new ArgumentOutOfRangeException();
            }
            TempHandles.Add(handle);
            return handle;
        }

        public void Dirty()
        {
            _isDirty = true;
        }

        public void FrameUpdate(RenderFrameEventArgs args)
        {
            if (!IsDirty)
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
                    #if GODOT
                    case OverlaySpace.ScreenSpace:
                        handle = new DrawingHandleScreen(MainCanvasItem);
                        break;
                    case OverlaySpace.WorldSpace:
                        handle = new DrawingHandleWorld(MainCanvasItem);
                        break;
                    #endif
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

        private void ClearDraw()
        {
            #if GODOT
            foreach (var item in CanvasItems)
            {
                VS.FreeRid(item);
            }

            VS.CanvasItemClear(MainCanvasItem);

            CanvasItems.Clear();
            #endif
        }

        private void UpdateZIndex()
        {
            #if GODOT
            if (MainCanvasItem == null)
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
            #endif
        }
    }
}
