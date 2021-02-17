using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using OpenToolkit.GraphicsLibraryFramework;
using Robust.Client.Utility;
using Robust.Shared.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using GlfwImage = OpenToolkit.GraphicsLibraryFramework.Image;

namespace Robust.Client.Graphics.Clyde
{
    internal sealed partial class Clyde
    {
        // These are actually Cursor* but we can't do that because no pointer generic arguments.
        // Need a queue to dispose cursors since the GLFW methods aren't allowed from non-main thread (finalizers).
        // And they also aren't re-entrant.
        private readonly ConcurrentQueue<IntPtr> _cursorDisposeQueue = new();

        private readonly Dictionary<StandardCursorShape, CursorImpl> _standardCursors =
            new();

        // Keep current active cursor around so it doesn't get garbage collected.
        private CursorImpl? _currentCursor;

        public ICursor GetStandardCursor(StandardCursorShape shape)
        {
            return _standardCursors[shape];
        }

        public unsafe ICursor CreateCursor(Image<Rgba32> image, Vector2i hotSpot)
        {
            fixed (Rgba32* pixPtr = image.GetPixelSpan())
            {
                var gImg = new GlfwImage(image.Width, image.Height, (byte*) pixPtr);
                var (hotX, hotY) = hotSpot;
                var ptr = GLFW.CreateCursor(gImg, hotX, hotY);

                return new CursorImpl(this, ptr, false);
            }
        }

        public unsafe void SetCursor(ICursor? cursor)
        {
            if (_currentCursor == cursor)
            {
                // Nothing has to be done!
                return;
            }

            if (cursor == null)
            {
                _currentCursor = null;
                GLFW.SetCursor(_glfwWindow, null);
                return;
            }

            if (!(cursor is CursorImpl impl) || impl.Owner != this)
            {
                throw new ArgumentException("Cursor is not created by this clyde instance.");
            }

            if (impl.Cursor == null)
            {
                throw new ObjectDisposedException(nameof(cursor));
            }

            _currentCursor = impl;
            GLFW.SetCursor(_glfwWindow, impl.Cursor);
        }

        private unsafe void FlushCursorDispose()
        {
            while (_cursorDisposeQueue.TryDequeue(out var cursor))
            {
                var ptr = (Cursor*) cursor;

                if (_currentCursor != null && ptr == _currentCursor.Cursor)
                {
                    // Currently active cursor getting disposed.
                    _currentCursor = null;
                }

                GLFW.DestroyCursor(ptr);
            }
        }

        private void InitCursors()
        {
            unsafe void AddStandardCursor(StandardCursorShape standardShape, CursorShape shape)
            {
                var ptr = GLFW.CreateStandardCursor(shape);

                var impl = new CursorImpl(this, ptr, true);

                _standardCursors.Add(standardShape, impl);
            }

            AddStandardCursor(StandardCursorShape.Arrow, CursorShape.Arrow);
            AddStandardCursor(StandardCursorShape.IBeam, CursorShape.IBeam);
            AddStandardCursor(StandardCursorShape.Crosshair, CursorShape.Crosshair);
            AddStandardCursor(StandardCursorShape.Hand, CursorShape.Hand);
            AddStandardCursor(StandardCursorShape.HResize, CursorShape.HResize);
            AddStandardCursor(StandardCursorShape.VResize, CursorShape.VResize);
        }

        private sealed class CursorImpl : ICursor
        {
            private readonly bool _standard;
            public Clyde Owner { get; }
            public unsafe Cursor* Cursor { get; private set; }

            public unsafe CursorImpl(Clyde clyde, Cursor* pointer, bool standard)
            {
                _standard = standard;
                Owner = clyde;
                Cursor = pointer;
            }

            ~CursorImpl()
            {
                DisposeImpl();
            }

            private unsafe void DisposeImpl()
            {
                Owner._cursorDisposeQueue.Enqueue((IntPtr) Cursor);
                Cursor = null;
            }

            public void Dispose()
            {
                if (_standard)
                {
                    throw new InvalidOperationException("Can't dispose standard cursor shape.");
                }

                GC.SuppressFinalize(this);
                DisposeImpl();
            }
        }
    }
}
