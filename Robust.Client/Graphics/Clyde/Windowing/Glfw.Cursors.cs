using System;
using System.Collections.Generic;
using System.Threading;
using OpenToolkit.GraphicsLibraryFramework;
using Robust.Client.Utility;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using GlfwImage = OpenToolkit.GraphicsLibraryFramework.Image;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private sealed unsafe partial class GlfwWindowingImpl
        {
            private readonly Dictionary<ClydeHandle, WinThreadCursorReg> _winThreadCursors = new();
            private readonly Dictionary<StandardCursorShape, CursorImpl> _standardCursors = new();

            public ICursor CursorGetStandard(StandardCursorShape shape)
            {
                return _standardCursors[shape];
            }

            public ICursor CursorCreate(Image<Rgba32> image, Vector2i hotSpot)
            {
                var cloneImg = new Image<Rgba32>(image.Width, image.Height);
                image.GetPixelSpan().CopyTo(cloneImg.GetPixelSpan());

                var id = _clyde.AllocRid();
                SendCmd(new CmdCursorCreate(cloneImg, hotSpot, id));

                return new CursorImpl(this, id, false);
            }

            private void WinThreadCursorCreate(CmdCursorCreate cmd)
            {
                var (img, (hotX, hotY), id) = cmd;

                fixed (Rgba32* pixPtr = img.GetPixelSpan())
                {
                    var gImg = new GlfwImage(img.Width, img.Height, (byte*) pixPtr);
                    var ptr = GLFW.CreateCursor(gImg, hotX, hotY);

                    _winThreadCursors.Add(id, new WinThreadCursorReg {Ptr = ptr});
                }

                img.Dispose();
            }

            public void CursorSet(WindowReg window, ICursor? cursor)
            {
                CheckWindowDisposed(window);

                var reg = (GlfwWindowReg) window;

                if (reg.Cursor == cursor)
                {
                    // Nothing has to be done!
                    return;
                }

                if (cursor == null)
                {
                    reg.Cursor = null;
                    SendCmd(new CmdWinCursorSet((nint) reg.GlfwWindow, default));
                    return;
                }

                var impl = (CursorImpl) cursor;
                DebugTools.Assert(impl.Owner == this);

                if (impl.Id == null)
                {
                    throw new ObjectDisposedException(nameof(cursor));
                }

                reg.Cursor = impl;
                SendCmd(new CmdWinCursorSet((nint) reg.GlfwWindow, impl.Id));
            }

            private void WinThreadWinCursorSet(CmdWinCursorSet cmd)
            {
                var window = (Window*) cmd.Window;
                Cursor* ptr = null;
                if (cmd.Cursor != default)
                    ptr = _winThreadCursors[cmd.Cursor].Ptr;

                if (_win32Experience)
                {
                    // Based on a true story.
                    Thread.Sleep(15);
                }

                GLFW.SetCursor(window, ptr);
            }

            private void WinThreadCursorDestroy(CmdCursorDestroy cmd)
            {
                var cursorReg = _winThreadCursors[cmd.Cursor];

                GLFW.DestroyCursor(cursorReg.Ptr);

                _winThreadCursors.Remove(cmd.Cursor);
            }

            private void InitCursors()
            {
                // Gets ran on window thread don't worry about it.

                void AddStandardCursor(StandardCursorShape standardShape, CursorShape shape)
                {
                    var id = _clyde.AllocRid();
                    var ptr = GLFW.CreateStandardCursor(shape);

                    var impl = new CursorImpl(this, id, true);

                    _standardCursors.Add(standardShape, impl);
                    _winThreadCursors.Add(id, new WinThreadCursorReg {Ptr = ptr});
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
                public GlfwWindowingImpl Owner { get; }
                public ClydeHandle Id { get; private set; }

                public CursorImpl(GlfwWindowingImpl clyde, ClydeHandle id, bool standard)
                {
                    _standard = standard;
                    Owner = clyde;
                    Id = id;
                }

                ~CursorImpl()
                {
                    DisposeImpl();
                }

                private void DisposeImpl()
                {
                    Owner.SendCmd(new CmdCursorDestroy(Id));
                    Id = default;
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

            public sealed class WinThreadCursorReg
            {
                public Cursor* Ptr;
            }
        }
    }
}
