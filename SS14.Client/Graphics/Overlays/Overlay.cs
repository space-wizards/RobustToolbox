using Godot;
using SS14.Client.Graphics.Drawing;
using SS14.Client.Interfaces.Graphics.Overlays;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VS = Godot.VisualServer;

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

        private bool _isDirty = true;

        private Godot.RID MainCanvasItem;

        private readonly List<Godot.RID> CanvasItems = new List<Godot.RID>();
        private readonly List<DrawingHandle> TempHandles = new List<DrawingHandle>();

        private bool Disposed = false;

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
            ClearDraw();
        }

        protected abstract void Draw(DrawingHandle handle);

        protected DrawingHandle NewHandle()
        {
            if (!Drawing)
            {
                throw new InvalidOperationException("Can only allocate new handles while drawing.");
            }

            var item = VS.CanvasItemCreate();
            VS.CanvasItemSetParent(item, MainCanvasItem);
            CanvasItems.Add(item);

            var handle = new DrawingHandle(item);
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
                Draw(new DrawingHandle(MainCanvasItem));
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

        public void AssignCanvasItem(RID canvasItem)
        {
            MainCanvasItem = canvasItem;
        }

        public void ClearCanvasItem()
        {
            ClearDraw();
            MainCanvasItem = null;
        }

        private void ClearDraw()
        {
            foreach (var item in CanvasItems)
            {
                VS.FreeRid(item);
            }

            VS.CanvasItemClear(MainCanvasItem);

            CanvasItems.Clear();
        }
    }
}
